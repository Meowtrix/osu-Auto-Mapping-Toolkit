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
        /// Process mp3 data for use in training and evaluating the machine learning models.
        /// </summary>
        /// <param name="mp3">Mp3 data in memory.</param>
        /// <returns>Data as matrix for the ML models. One row for a data point in the sequence.</returns>
        public static Matrix<double> ProcessMp3(Stream mp3)
        {
            throw new NotImplementedException();
        }
    }
}
