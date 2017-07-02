using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Meowtrix.osuAMT.MusicProcess
{
    using hip_t = IntPtr;

    public static class MusicProcess
    {
        const string LAME = "libmp3lame";

        [DllImport(LAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern hip_t hip_decode_init();

        [DllImport(LAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int hip_decode_exit(hip_t gfp);

        [DllImport(LAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int hip_decode1(hip_t gfp, byte* buffer, [param: MarshalAs(UnmanagedType.SysInt)] int len, short* pcm_l, short* pcm_r);

        /// <summary>
        /// Delegate for an FFT implementation, which runs FFT on the data buffer in place, block by block.
        /// Block size is fixed to 256.
        /// </summary>
        /// <param name="data">Data buffer. Must be in size of a multiple of 256.</param>
        public delegate void FFT256(short[] data);

        /// <summary>
        /// Set this to change the FFT implementation used; default to a managed version FFT implementation.
        /// User may consider provide some optimized or accelerated implementation wrapping MKL, cuFFT or so on.
        /// </summary>
        public static FFT256 FFT256Implementation = ManagedFFT256;

        /// <summary>
        /// Process mp3 data for use in training and evaluating the machine learning models.
        /// </summary>
        /// <param name="mp3">Mp3 data in memory.</param>
        /// <returns>Matrix as data for the ML models. One row for a data point in the sequence.</returns>
        public static short[,] ProcessMp3(byte[] mp3)
        {
            var leftFrames = new List<short[]>();
            var leftBuff = new short[65536];
            var rightFrames = new List<short[]>();
            var rightBuff = new short[65536];
            int totalLength = 0;
            var gfp = hip_decode_init();

            // Use LAME to decode the mp3 data.
            unsafe
            {
                fixed (byte* input = mp3)
                fixed (short* pcm_l = leftBuff)
                fixed (short* pcm_r = rightBuff)
                {
                    int frameLength;
                    while ((frameLength = hip_decode1(gfp, input, mp3.Length, pcm_l, pcm_r)) > 0)
                    {
                        var leftFrame = new short[frameLength];
                        var rightFrame = new short[frameLength];
                        Array.Copy(leftBuff, leftFrame, frameLength);
                        Array.Copy(rightBuff, rightFrame, frameLength);
                        leftFrames.Add(leftFrame);
                        rightFrames.Add(rightFrame);
                        totalLength += frameLength;
                    }
                    if (frameLength < 0) throw new Exception("MP3 file decoding failed.");
                }
            }

            // Padding the size so that it's a multiple of 256.
            var leftPCM = new short[(totalLength + 255) & ~255];
            var rightPCM = new short[(totalLength + 255) & ~255];
            leftFrames.Aggregate(0, (n, frame) => { Buffer.BlockCopy(frame, 0, leftPCM, n, frame.Length * sizeof(short)); return n + frame.Length; });
            rightFrames.Aggregate(0, (n, frame) => { Buffer.BlockCopy(frame, 0, rightPCM, n, frame.Length * sizeof(short)); return n + frame.Length; });

            // Execute FFT.
            FFT256Implementation(leftPCM);
            FFT256Implementation(rightPCM);

            // Reshape the result into desired dimension.
            var reshaped = new short[(totalLength + 255) / 256, 256];
            for (int i = 0; i < (totalLength + 255) / 256; ++i)
            {
                // Only copying first 128 point of each FFT result since we only have lower half
                // frequency region data be reasonable with real part input only.
                Buffer.BlockCopy(leftPCM, i * 256 * sizeof(short), reshaped, i * 256, 128 * sizeof(short));
                Buffer.BlockCopy(rightPCM, i * 256 * sizeof(short), reshaped, i * 256 + 128, 128 * sizeof(short));
            }

            return reshaped;
        }

        private static float[] cosTable;
        private static float[] sinTable;
        private static byte[] bitReverseTable;

        static MusicProcess()
        {
            // Print out sin and cos tables.
            cosTable = new float[128];
            sinTable = new float[128];
            for (int i = 0; i < 128; ++i)
            {
                cosTable[i] = (float)Math.Cos(2 * i * Math.PI / 256);
                sinTable[i] = (float)Math.Sin(2 * i * Math.PI / 256);
            }

            // Print out bit reverse lookup table.
            bitReverseTable = new byte[256];
            byte bitReverse(byte b_orig)
            {
                uint b = b_orig;
                b = (b & 0xF0) >> 4 | (b & 0x0F) << 4;
                b = (b & 0xCC) >> 2 | (b & 0x33) << 2;
                b = (b & 0xAA) >> 1 | (b & 0x55) << 1;
                return (byte)b;
            }
            for (int i = 0; i < 256; ++i)
                bitReverseTable[i] = bitReverse((byte)i);
        }

        /// <summary>
        /// Algorithm derived from Chapter 30, Introduction to Algorithms, 3rd Edition.
        /// </summary>
        /// <param name="data">See FFT256.</param>
        internal static void ManagedFFT256(short[] data)
        {
            float[] real = new float[256];
            float[] imag = new float[256];
            for (int offset = 0; offset < data.Length; offset += 256)
            {
                // Copy data and convert to floats.
                for (int i = 0; i < 256; ++i)
                {
                    real[bitReverseTable[i]] = data[offset + i];
                    imag[i] = 0;
                }

                // Do complex-to-complex FFT.
                for (int m = 2; m <= 256; m *= 2)
                    for (int k = 0; k < 256; k += m)
                        for (int j = 0; j < m / 2; ++j)
                        {
                            // w = Exp(2 * PI * j * i / m)
                            var w_real = cosTable[j * 256 / m];
                            var w_imag = sinTable[j * 256 / m];
                            // t = w * A[k + j + m / 2]
                            var t_real = w_real * real[k + j + m / 2] - w_imag * imag[k + j + m / 2];
                            var t_imag = w_real * imag[k + j + m / 2] + w_imag * real[k + j + m / 2];
                            // u = A[k + j]
                            var u_real = real[k + j];
                            var u_imag = imag[k + j];
                            // A[k + j] = u + t
                            real[k + j] = u_real + t_real;
                            imag[k + j] = u_imag + t_imag;
                            // A[k + j + m / 2] = u - t
                            real[k + j + m / 2] = u_real - t_real;
                            imag[k + j + m / 2] = u_imag - t_imag;
                        }

                // Calculate magnitude, round and write back.
                for (int i = 0; i < 256; ++i)
                    data[offset + i] = (short)((int)Math.Sqrt(real[i] * real[i] + imag[i] * imag[i]) / 256);
            }
        }
    }
}
