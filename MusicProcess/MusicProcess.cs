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
        internal static extern unsafe int hip_decode1(hip_t gfp, byte* buffer, UIntPtr len, short* pcm_l, short* pcm_r);

        /// <summary>
        /// Set this to change the FFT implementation used; default to a managed version FFT implementation.
        /// User may consider provide some optimized or accelerated implementation wrapping MKL, cuFFT or so on.
        /// </summary>
        public static FFT256 FFT256Implementation = ManagedFFT256.Execute;

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
                    while ((frameLength = hip_decode1(gfp, input, new UIntPtr((uint)mp3.Length), pcm_l, pcm_r)) > 0)
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
    }
}
