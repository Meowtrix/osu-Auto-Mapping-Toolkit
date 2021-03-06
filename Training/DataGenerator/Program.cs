﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Meowtrix.osuAMT.MusicProcess;

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

            var songs = new ConcurrentBag<Archive>(dir.EnumerateDirectories().Select(d => new Folder(d))
                .AsEnumerable<Archive>()
                .Concat(dir.EnumerateFiles("*.osz", SearchOption.AllDirectories).Select(f => new OszArchive(f))));
            int committedSongs = 0, totalSongs = songs.Count;

            output = new BinaryWriter(File.OpenWrite("output.bin"));

            var workers = new Thread[parallel];
            for (int i = 0; i < parallel; i++)
            {
                workers[i] = new Thread(() =>
                {
                    while (songs.TryTake(out Archive archive))
                    {
                        Interlocked.Increment(ref committedSongs);
                        try { ProcessData(archive); }
                        catch (Exception e)
                        {
                            Console.WriteLine(new Exception($"Error processing {archive.Name}. Ignoring.", e).ToString());
                        }
                    }
                });
                workers[i].Start();
            }

            bool finished = false;

            var reportThread = new Thread(() =>
            {
                while (!Volatile.Read(ref finished))
                {
                    Console.WriteLine($"{Volatile.Read(ref committedSongs)}/{totalSongs} songs committed for processing.");
                    Thread.Sleep(1000);
                }
            });
            reportThread.Start();

            foreach (var thread in workers)
                thread.Join();

            Volatile.Write(ref finished, true);
            reportThread.Join();

            output.Flush();
            output.Dispose();

            return 0;
        }

        static BinaryWriter output;

        static void ProcessData(Archive archive)
        {
            var timingList = new List<(double time, double beatLength, int sectionLength, bool kiai)>();
            string audioFile = null;
            foreach (var stream in archive.OpenOsuFiles())
                using (var reader = new StreamReader(stream))
                {
                    while (reader.ReadLine().Trim() != "[General]") ;
                    string audio;
                    while (true)
                    {
                        string[] split = reader.ReadLine().Split(':');
                        if (split[0] == "AudioFilename")
                        {
                            audio = split[1].Trim();
                            break;
                        }
                    }
                    if (audioFile == null)
                        audioFile = audio;
                    else if (audioFile != audio)
                        continue;

                    while (reader.ReadLine().Trim() != "[TimingPoints]") ;
                    string timing;
                    while (!string.IsNullOrWhiteSpace(timing = reader.ReadLine()))
                    {
                        string[] split = timing.Split(',');
                        double time = double.Parse(split[0]);
                        double beatLength = double.Parse(split[1]);

                        int sectionLength = 4;
                        if (split.Length >= 3)
                            sectionLength = split[2][0] == '0' ? 4 : int.Parse(split[2]);

                        bool timingChange = true;
                        if (split.Length >= 7)
                            timingChange = split[6][0] == '1';

                        bool kiai = false;
                        if (split.Length >= 8)
                            kiai = (int.Parse(split[7]) & 1) > 0;

                        int i = 0;
                        for (i = 0; i < timingList.Count; i++)
                        {
                            var point = timingList[i];
                            if (point.time == time) i = -1;
                            if (point.time >= time) break;
                        }

                        if (i == -1) continue;
                        if (timingChange || i > 0 && (kiai != timingList[i - 1].kiai))
                            timingList.Insert(i, (time, beatLength, sectionLength, kiai));
                    }
                }
            if (timingList.Count == 0) return;

            float[] audiodata;
            using (var reader = new BinaryReader(archive.OpenFile(audioFile)))
                try
                {
                    audiodata = MusicProcesser.ProcessData(reader.ReadBytes((int)reader.BaseStream.Length)).ToArray();
                }
                catch (MusicProcessException) { return; }

            (archive as IDisposable)?.Dispose();

            int sampleCount = audiodata.Length + 1;
            byte[] data = new byte[sampleCount];
            timingList.Add((double.MaxValue, double.NaN, 0, false)); //fence item

            int beat = 0;
            double length = double.MaxValue;
            for (int i = 0; i < timingList.Count - 1; i++)
            {
                var thisPoint = timingList[i];
                double nextTime = timingList[i + 1].time - 2; //as epsilon

                if (thisPoint.beatLength > 0)
                {
                    beat = 0;
                    length = thisPoint.beatLength;
                }

                for (double time = thisPoint.time; time < nextTime; time += length)
                {
                    int sample = (int)(time * 44.1);
                    if (sample >= data.Length) break;
                    if (sample < 0) continue;

                    byte signature = 1;
                    if (beat % thisPoint.sectionLength == 0) signature |= 2;
                    if (thisPoint.kiai) signature |= 4;

                    data[sample] = signature;
                    beat++;
                }
            }

            byte[] audiobytes = new byte[audiodata.Length * sizeof(float)];
            Buffer.BlockCopy(audiodata, 0, audiobytes, 0, audiobytes.Length);

            var idstr = Regex.Match(archive.Name, @"\d+");
            int id = -1;
            if (idstr.Success)
                id = int.Parse(idstr.Value);

            lock (output)
            {
                output.Write(id);
                output.Write(sampleCount);
                output.Write(audiobytes);
                output.Write(data);
            }
        }
    }
}