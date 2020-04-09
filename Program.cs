using System;

namespace CudaTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var o = new CudafyMapReduce();

            string text = System.IO.File.ReadAllText(@"C:\Users\mylif\Downloads\cantrbry\alice29.txt");

            string[] sentences = text.Split(new char[] { '\n' });

            for (int i = 0; i < sentences.Length; i++)
            {
                var res = o.Run(sentences[i]);
                for (int i = 0; i < res.Length; i++)
                {
                    if (res[i].Value != 0)
                    {
                        byte[] bytes = BitConverter.GetBytes(res[i]);
                        Console.Write(BitConverter.ToString(bytes)+" and ");
                        Console.Write(res[i].Value);
                    }
                }
            }
            Console.ReadKey();
        }
    }
}
