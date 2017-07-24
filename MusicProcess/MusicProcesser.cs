using NLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Meowtrix.osuAMT.MusicProcess
{
    public class MusicProcessException : Exception
    {
        public MusicProcessException() : base() { }

        public MusicProcessException(string message) : base(message) { }

        public MusicProcessException(string message, Exception innerException) : base(message, innerException) { }
    }

    public static class MusicProcesser
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
            if (file.SampleRate != 44100) throw new MusicProcessException("Sample rate is not 44100. Consider conversion.");
            var sampleCount = (int)(file.Length / (file.Channels * sizeof(float)));
            var downSampledSampleCount = (sampleCount + 255) / 256; // Ceiling to multiple of 256
            var pcm = new float[downSampledSampleCount * 256];

            try { file.ReadSamples(pcm, 0, sampleCount); } catch { throw new MusicProcessException("NLayer Error."); }

            // Execute FFT.
            FFT256Implementation(pcm);

            // Reshape the result into desired dimension.
            var reshaped = new float[downSampledSampleCount, 64];
            // Only copying first 64 point of each FFT result: cutting off frequency data above ~11kHz should be ok for normal "music".
            for (int i = 0; i < downSampledSampleCount; ++i)
                Buffer.BlockCopy(pcm, i * 256 * sizeof(float), reshaped, i * 64 * sizeof(float), 64 * sizeof(float));

            return reshaped;
        }
    }
}
