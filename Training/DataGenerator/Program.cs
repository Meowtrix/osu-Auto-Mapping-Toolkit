using System;
using System.IO;

namespace Meowtrix.osuAMT.Training.DataGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
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

            return 0;
        }
    }
}