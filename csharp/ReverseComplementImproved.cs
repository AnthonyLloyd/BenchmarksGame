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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

struct section { public byte[] data; public int length; }
struct sequence { public List<section> data; public int startHead,start,end; }

public static class revcompImproved
{
    const int READER_BUFFER_SIZE = 1024*16;
    static BlockingCollection<section> readQue = new BlockingCollection<section>();

    static BlockingCollection<sequence> writeQue = new BlockingCollection<sequence>();

    static byte[] getBuffer()
    {
        return new byte[READER_BUFFER_SIZE];
    }

    static void returnBuffer(byte[] bytes)
    {

    }

    static void Reader()
    {
        using (var stream = Console.OpenStandardInput())
        {
            byte[] buffer;
            int bytesRead;
            for (;;)
            {
                buffer = getBuffer();
                bytesRead = stream.Read(buffer, 0, READER_BUFFER_SIZE);
                if (bytesRead == 0) break;
                readQue.Add(new section { data = buffer, length = bytesRead });
            }
            readQue.CompleteAdding();
        }
    }

    static void Reverser()
    {
        const byte LF = 10;
        const byte GT = (byte)'>';
        bool doMapping = false;

        while (!readQue.IsCompleted)
        {
            var section = readQue.Take();

            var bytes = section.data;
            var len = section.length;
            for (int i = 0; i < bytes.Length && i < len; i++)
            {
                var b = bytes[i];
                if (b == GT)
                {
                    doMapping = false;
                }
                else if (b == LF)
                {
                    doMapping = true;
                }
                else if (doMapping)
                {
                    switch(b)
                    {
                        case (byte)'A': bytes[i] = (byte)'T'; break;
                        case (byte)'B': bytes[i] = (byte)'V'; break;
                        case (byte)'C': bytes[i] = (byte)'G'; break;
                        case (byte)'D': bytes[i] = (byte)'H'; break;
                        case (byte)'G': bytes[i] = (byte)'C'; break;
                        case (byte)'H': bytes[i] = (byte)'D'; break;
                        case (byte)'K': bytes[i] = (byte)'M'; break;
                        case (byte)'M': bytes[i] = (byte)'K'; break;
                        case (byte)'R': bytes[i] = (byte)'Y'; break;
                        case (byte)'T': bytes[i] = (byte)'A'; break;
                        case (byte)'V': bytes[i] = (byte)'B'; break;
                        case (byte)'Y': bytes[i] = (byte)'R'; break;
                        case (byte)'a': bytes[i] = (byte)'T'; break;
                        case (byte)'b': bytes[i] = (byte)'V'; break;
                        case (byte)'c': bytes[i] = (byte)'G'; break;
                        case (byte)'d': bytes[i] = (byte)'H'; break;
                        case (byte)'g': bytes[i] = (byte)'C'; break;
                        case (byte)'h': bytes[i] = (byte)'D'; break;
                        case (byte)'k': bytes[i] = (byte)'M'; break;
                        case (byte)'m': bytes[i] = (byte)'K'; break;
                        case (byte)'r': bytes[i] = (byte)'Y'; break;
                        case (byte)'t': bytes[i] = (byte)'A'; break;
                        case (byte)'v': bytes[i] = (byte)'B'; break;
                        case (byte)'y': bytes[i] = (byte)'R'; break;
                    }
                }
            }
            writeQue.Add(section);
        }
        writeQue.CompleteAdding();
    }

    static void Writer()
    {
        using (var stream = Console.OpenStandardOutput())
        {
            while (!writeQue.IsCompleted)
            {
                var section = writeQue.Take();
                stream.Write(section.data, 0, section.length);
                returnBuffer(section.data);
            }
        }
    }

    public static void Main(string[] args)
    {
        Task.Run((Action)Reader);
        Task.Run((Action)Reverser);
        Task.Run((Action)Writer).Wait();
    }
}