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
            var reshaped = new short[(totalLength + 255) / 256, 512];
            for (int i = 0; i < (totalLength + 255) / 256; ++i)
            {
                Buffer.BlockCopy(leftPCM, i * 256, reshaped, i * 512, 256);
                Buffer.BlockCopy(rightPCM, i * 256, reshaped, i * 512 + 256, 256);
            }

            return reshaped;
        }

        private static void ManagedFFT256(short[] data)
        {
            throw new NotImplementedException();
        }
    }
}
