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
            byte[] mp3;
            using (var stream = new FileStream(Path.Combine(
                Path.GetDirectoryName(typeof(MusicProcessTest).GetTypeInfo().Assembly.Location), file), FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
                mp3 = reader.ReadBytes((int)stream.Length);
            var result = MusicProcess.ProcessMp3(mp3);
            return;
        }
    }
}
