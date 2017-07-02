using System;
using System.Collections.Generic;
using System.Text;

namespace Meowtrix.osuAMT.MusicProcess
{
    /// <summary>
    /// Delegate for an FFT implementation, which runs FFT on the data buffer in place, block by block.
    /// Block size is fixed to 256.
    /// </summary>
    /// <param name="data">Data buffer. Must be in size of a multiple of 256.</param>
    public delegate void FFT256(float[] data);

    public static class ManagedFFT256
    {
        private static float[] cosTable;
        private static float[] sinTable;
        private static byte[] bitReverseTable;

        static ManagedFFT256()
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
        public static void Execute(float[] data)
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
                    data[offset + i] = (float)Math.Sqrt(real[i] * real[i] + imag[i] * imag[i]) / 256;
            }
        }
    }
}
