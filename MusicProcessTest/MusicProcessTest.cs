using Meowtrix.osuAMT.MusicProcess;
using System;
using System.IO;
using System.Linq;
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
            float[,] result;
            using (var stream = new FileStream(Path.Combine(
                Path.GetDirectoryName(typeof(MusicProcessTest).GetTypeInfo().Assembly.Location), file), FileMode.Open, FileAccess.Read))
                result = MusicProcesser.ProcessMp3(stream);
            // What we can do to verify the result?
            return;
        }
    }
}
