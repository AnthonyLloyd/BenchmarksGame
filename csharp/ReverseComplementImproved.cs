/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   Contributed by Peperud

   Attempt at introducing concurrency.
   Ideas and code borrowed from various other contributions and places. 

   TODO 
        Better way to reverse complement the block's body without having 
        to flatten it first.
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

struct section {public byte[] data; public int length; }

public static class revcompImproved
{
    static BlockingCollection<section> readQue = new BlockingCollection<section>();
    static BlockingCollection<section> writeQue = new BlockingCollection<section>();

    static readonly int READER_BUFFER_SIZE = 1024 * 1024 * 16;

    static readonly byte[] map = new byte[256];
    static readonly byte LF = 10;
    const string Seq = "ABCDGHKMRTVYabcdghkmrtvy";
    const string Rev = "TVGHCDMKYABRTVGHCDMKYABR";

    static byte[] getBuffer()
    {
        return new byte[READER_BUFFER_SIZE];
    }
    static byte[] returnBuffer(byte[] bytes)
    {

    }

    static void Reader()
    {
        using (var inS = Console.OpenStandardInput())
        {
            Console.WriteLine("does length work="+inS.Length);
            byte[] buffer;
            int bytesRead;
            for(;;)
            {
                buffer = getBuffer();
                bytesRead = inS.Read(buf, 0, READER_BUFFER_SIZE);
                if(bytesRead==0) break;
                readQue.Add(new section { data=buffer, length=bytesRead });
            }
            readQue.CompleteAdding();
        }
    }

    static void Writer()
    {
        using (var outS = Console.OpenStandardOutput())
        {
            while (!readQue.IsCompleted)
            {
                var section = readQue.Take();
                outS.Write()
            }
        }
    }

    static void Reverser()
    {

    }

    static void Main(string[] args)
    {
        for (byte i = 0; i < 255; i++)
        {
            comp[i] = i;
        }
        for (int i = 0; i < Seq.Length; i++)
        {
            comp[(byte)Seq[i]] = (byte)Rev[i];
        }
        comp[LF] = 0;
        comp[(byte)' '] = 0;

        Task.Run(Reader);
        Task.Run(Reverser);
        Task.Run(Writer).Wait();
    }
}