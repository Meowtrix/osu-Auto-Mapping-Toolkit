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

        private FileInfo fileinfo;

        public OszArchive(FileInfo file)
        {
            fileinfo = file;
            string name = file.Name;
            Name = name.EndsWith(".osz") ? name.Substring(0, name.Length - 4) : name;
        }

        public override string Name { get; }

        public void Dispose() => archive.Dispose();

        private void EnsureArchiveOpened()
        {
            if (archive == null)
                archive = new ZipArchive(fileinfo.OpenRead(), ZipArchiveMode.Read, false);
        }

        public override Stream OpenFile(string filename)
        {
            EnsureArchiveOpened();
            return archive.GetEntry(filename).Open();
        }

        public override IEnumerable<Stream> OpenOsuFiles()
        {
            EnsureArchiveOpened();
            return archive.Entries.Where(x => x.Name.EndsWith(".osu")).Select(e => e.Open());
        }
    }
}
