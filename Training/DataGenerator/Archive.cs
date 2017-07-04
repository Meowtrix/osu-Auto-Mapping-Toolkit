using System.Collections.Generic;
using System.IO;

namespace Meowtrix.osuAMT.Training.DataGenerator
{
    abstract class Archive
    {
        public abstract string Name { get; }
        public abstract IEnumerable<Stream> OpenOsuFiles();
        public abstract Stream OpenFile(string filename);
    }
}
