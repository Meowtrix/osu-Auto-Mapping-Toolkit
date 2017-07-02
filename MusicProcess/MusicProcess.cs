using NLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Meowtrix.osuAMT.MusicProcess
{
    public static class MusicProcess
    {
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
        public static float[,] ProcessMp3(Stream mp3)
        {
            var file = new MpegFile(mp3) { StereoMode = StereoMode.DownmixToMono };
            var sampleCount = (int)(file.Length / (file.Channels * sizeof(float)));
            var paddedSampleCount = (sampleCount + 255) / 256; // Ceiling to multiple of 256
            var pcm = new float[sampleCount];
            file.ReadSamples(pcm, 0, sampleCount);

            // Execute FFT.
            FFT256Implementation(pcm);

            // Reshape the result into desired dimension.
            var reshaped = new float[paddedSampleCount, 64];
            // Only copying first 64 point of each FFT result: cutting off frequency data above ~11kHz should be ok for normal "music".
            for (int i = 0; i < paddedSampleCount; ++i)
                Buffer.BlockCopy(pcm, i * 256 * sizeof(float), reshaped, i * 64 * sizeof(float), 64 * sizeof(float));

            return reshaped;
        }
    }
}
