using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Project1
{
    class MapReduceStart
    {
        private static List<string> Lines;

        static void Main(string[] args)
        {
            string path = @"C:\Users\mylif\Downloads\cantrbry\alice29.txt";
            Lines = ReadLines(path);
            var obj = new CudafyMapReduce();
            var res = obj.Run(Lines);

            foreach(var elem in res)
            {
                Console.WriteLine($"Word: {elem.Key} has freqeuncy: {elem.Value}");
            }

            Console.ReadKey();
        }

        private static List<string> ReadLines(string path)
        {
            Lines = new List<string>();
            using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        Lines.Add(line);
                    }
                }
            }
            return Lines;
        }
        
    }
}
