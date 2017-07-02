using Meowtrix.osuAMT.MusicProcess;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MusicProcessTest
{
    public class ManagedFFT256Test
    {
        [Fact]
        public void ConstantInput()
        {
            var buffer = new float[256];
            for (int i = 0; i < 256; ++i)
                buffer[i] = short.MaxValue;
            ManagedFFT256.Execute(buffer);
            Assert.InRange(buffer[0], 32765, 32768);
            for (int i = 1; i < 256; ++i) Assert.Equal(0, buffer[i], 2);
        }

        [Fact]
        public void HighFreqInput()
        {
            var buffer = new float[256];
            for (int i = 0; i < 256; ++i)
                buffer[i] = i % 2 == 0 ? short.MaxValue : short.MinValue;
            ManagedFFT256.Execute(buffer);
            Assert.InRange(buffer[128], 32765, 32768);
            for (int i = 1; i < 128; ++i) Assert.Equal(0, buffer[i], 2);
            for (int i = 129; i < 256; ++i) Assert.Equal(0, buffer[i], 2);
        }

        [Fact]
        public void LongConstantInput()
        {
            var buffer = new float[256 * 65536];
            for (int i = 0; i < 256 * 65536; ++i)
                buffer[i] = short.MaxValue;
            ManagedFFT256.Execute(buffer);
            for (int i = 0; i < 256 * 65536; i += 256)
            {
                Assert.InRange(buffer[i], 32765, 32768);
                for (int j = 1; j < 256; ++j) Assert.Equal(0, buffer[i + j], 2);
            }
        }

        static float[] staticBuffer = new float[256 * 65536];

        [Fact]
        public void PerformanceTest()
        {
            ManagedFFT256.Execute(staticBuffer);
        }
    }
}
