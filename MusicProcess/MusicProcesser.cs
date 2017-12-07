using ManagedBass;
using MathNet.Numerics.LinearAlgebra;
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
        /// Process audio data for use in training and evaluating the machine learning models.
        /// </summary>
        /// <param name="data">Audio data in memory.</param>
        /// <returns>Data as matrix for the ML models. One row for a data point in the sequence.</returns>
        public unsafe static Vector<float> ProcessData(byte[] data)
        {
            if (!Bass.Init(0))
                throw new MusicProcessException("BASS Error: Initialization failed.");

            var bassStream = Bass.CreateStream(data, 0, data.Length, BassFlags.Float | BassFlags.Mono | BassFlags.Decode | BassFlags.Prescan);
            if (bassStream == 0)
                throw new MusicProcessException($"BASS Error: Failed to create stream with error code {Bass.LastError}.");
            
            if (Math.Abs(Bass.ChannelGetAttribute(bassStream, ChannelAttribute.Frequency) - 44100) > 1e-3)
                throw new MusicProcessException("Audio with non-44.1k sample rate not supported.");

            var decodedLength = (int)Bass.ChannelGetLength(bassStream);
            var result = Vector<float>.Build.Dense(decodedLength);
            Bass.ChannelGetData(bassStream, result.AsArray(), decodedLength);

            Bass.StreamFree(bassStream);
            Bass.Free();
            return result;
        }
    }
}
