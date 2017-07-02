using Meowtrix.osuAMT.MusicProcess;
using System;
using System.Linq;
using Xunit;

namespace MusicProcessTest
{
    public class ManagedFFT256Test
    {
        [Fact]
        public void ConstantInput()
        {
            var buffer = new short[256];
            for (int i = 0; i < 256; ++i)
                buffer[i] = short.MaxValue;
            ManagedFFT256.Execute(buffer);
            Assert.InRange(buffer[0], 32765, 32768);
            Assert.Equal(Enumerable.Repeat((short)0, 255), new ArraySegment<short>(buffer, 1, 255));
        }

        [Fact]
        public void HighFreqInput()
        {
            var buffer = new short[256];
            for (int i = 0; i < 256; ++i)
                buffer[i] = i % 2 == 0 ? short.MaxValue : short.MinValue;
            ManagedFFT256.Execute(buffer);
            Assert.InRange(buffer[128], 32765, 32768);
            Assert.Equal(Enumerable.Repeat((short)0, 127), new ArraySegment<short>(buffer, 1, 127));
            Assert.Equal(Enumerable.Repeat((short)0, 127), new ArraySegment<short>(buffer, 129, 127));
        }
    }
}
