using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Meowtrix.osuAMT.Training.DataGenerator
{
    class Folder : Archive
    {
        private DirectoryInfo directory;

        public Folder(DirectoryInfo directory)
        {
            this.directory = directory;
        }

        public override Stream OpenFile(string filename)
            => File.OpenRead(Path.Combine(directory.FullName, filename));

        public override IEnumerable<Stream> OpenOsuFiles()
            => directory.EnumerateFiles("*.osu", SearchOption.AllDirectories).Select(f => f.OpenRead());
    }
}
