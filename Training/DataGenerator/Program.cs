using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Meowtrix.osuAMT.Training.DataGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Use root songs folder as parameter.");
                return -1;
            }

            var dir = new DirectoryInfo(args[0]);
            if (!dir.Exists)
            {
                Console.WriteLine("Unknown root folder.");
                return -1;
            }

            int parallel;
            if (args.Length >= 3)
            {
                if (args[1] != "-n")
                {
                    Console.WriteLine("Use -n for parallel count.");
                    return -1;
                }
                parallel = int.Parse(args[2]);
            }
            else
            {
                parallel = Environment.ProcessorCount;
            }

            var songs = dir.EnumerateDirectories().Select(d => new Folder(d))
                .AsEnumerable<Archive>()
                .Concat(dir.EnumerateFiles("*.osz", SearchOption.AllDirectories).Select(f => new OszArchive(f.OpenRead(), f.Name)))
                .ToList();
            var committedSongs = 0;

            var reportTimer = new Timer((obj) => Console.WriteLine($"{committedSongs}/{songs.Count} songs committed for processing."), null, 0, 10000);

            var workers = new Thread[parallel];
            using (var enumerator = songs.GetEnumerator())
                for (int i = 0; i < parallel; i++)
                {
                    workers[i] = new Thread(() =>
                    {
                        while (true)
                        {
                            Archive archive;
                            lock (songs)
                            {
                                if (!enumerator.MoveNext()) break;
                                archive = enumerator.Current;
                                committedSongs++;
                            }
                            ProcessData(archive);
                        }
                    });
                    workers[i].Start();
                }

            foreach (var thread in workers)
                thread.Join();

            return 0;
        }

        static void ProcessData(Archive archive)
        {

        }
    }
}