using MathNet.Numerics.IntegralTransforms;
using Meowtrix.osuAMT.MusicProcess;
using System;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using System.Reflection;
using System.Resources;
using Xunit;

namespace MusicProcessTest
{
    public class MusicProcessTest
    {
        [Theory]
        [InlineData("Resource/440Hz-5sec.mp3")]
        public void Test(string file)
        {
            using (var br = new BinaryReader(new FileStream(
                Path.Combine(Path.GetDirectoryName(typeof(MusicProcessTest).GetTypeInfo().Assembly.Location), file),
                FileMode.Open, FileAccess.Read)))
            {
                var result = MusicProcesser.ProcessData(br.ReadBytes((int)br.BaseStream.Length));
                // How can we check the result?
            }
        }
    }
}
