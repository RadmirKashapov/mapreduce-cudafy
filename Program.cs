using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MapReduceCudafy
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"C:\Users\mylif\Downloads\cantrbry\test.txt";
            //string path = @"C:\Users\mylif\Downloads\cantrbry\alice29.txt";
            //string path = "input.txt";
            //string path = @"C:\Users\mylif\Desktop\asyoulik.txt";
            //Lines = ReadLines(path);
            //var obj = new CudafyMapReduce();
            //var res = obj.Run(Lines);

            var res = ReadLines(path);
            //foreach (var elem in res)
            //{
            //    Console.WriteLine($"Word: {elem.Key} has freqeuncy: {elem.Value}");
            //}

            Console.ReadKey();
        }

        private static Dictionary<string, int> ReadLines(string path)
        {
            int LineBlockSize = 30000;
            int LineCount = 0;
            List<string> Lines = new List<string>();
            Dictionary<string, int> frequencyDict = new Dictionary<string, int>();
            using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    string line;
                    while (!streamReader.EndOfStream)
                    {
                        LineCount++;
                        line = streamReader.ReadLine();
                        Lines.Add(line.Trim().ToLower());
                        if (Lines.Count == LineBlockSize)
                        {
                            var obj = new CudafyMapReduce();
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();
                            var res = obj.Run(Lines);
                            stopwatch.Stop();
                            foreach (var elem in res)
                            {
                                if (elem.Key == null || elem.Value == 0) continue;
                                Console.WriteLine($"Word: {elem.Key} has freqeuncy: {elem.Value}");
                                frequencyDict[elem.Key] = elem.Value;
                            }
                            TimeSpan ts = stopwatch.Elapsed;
                            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                                            ts.Hours, ts.Minutes, ts.Seconds,
                                                            ts.Milliseconds / 10);
                            Lines = new List<string>();
                            Console.WriteLine("RunTime " + elapsedTime);
                        }
                        if(streamReader.EndOfStream && LineCount % LineBlockSize != 0)
                        {
                            var obj = new CudafyMapReduce();
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();
                            var res = obj.Run(Lines);
                            stopwatch.Stop();
                            foreach (var elem in res)
                            {
                                if (elem.Key == null || elem.Value == 0) continue;
                                Console.WriteLine($"Word: {elem.Key} has freqeuncy: {elem.Value}");
                                frequencyDict[elem.Key] = elem.Value;
                            }
                            TimeSpan ts = stopwatch.Elapsed;
                            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                                            ts.Hours, ts.Minutes, ts.Seconds,
                                                            ts.Milliseconds / 10);
                            Console.WriteLine("RunTime " + elapsedTime);
                        }
                    }
                }
            }
            return frequencyDict;
        }

    }
}
