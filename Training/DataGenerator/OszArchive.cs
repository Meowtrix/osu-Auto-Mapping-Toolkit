using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Meowtrix.osuAMT.Training.DataGenerator
{
    class OszArchive : Archive, IDisposable
    {
        public ZipArchive archive;

        public OszArchive(Stream stream)
        {
            archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
        }

        public void Dispose() => archive.Dispose();

        public override Stream OpenFile(string filename) => archive.GetEntry(filename).Open();

        public override IEnumerable<Stream> OpenOsuFiles() => archive.Entries.Where(x => x.Name.EndsWith(".osz")).Select(e => e.Open());
    }
}
