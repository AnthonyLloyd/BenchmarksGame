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

class page { public byte[] data; public int length; }
class sequence { public List<page> pages; public int startHeader, endExclusive; }

public static class revcompImproved
{
    const int READER_BUFFER_SIZE = 1024 * 16;
    static BlockingCollection<page> readQue = new BlockingCollection<page>();
    static BlockingCollection<sequence> groupQue = new BlockingCollection<sequence>();
    static BlockingCollection<sequence> writeQue = new BlockingCollection<sequence>();

    static byte[] borrowBuffer()
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
                buffer = borrowBuffer();
                bytesRead = stream.Read(buffer, 0, READER_BUFFER_SIZE);
                if (bytesRead == 0) break;
                readQue.Add(new page { data = buffer, length = bytesRead });
            }
            readQue.CompleteAdding();
        }
    }

    static void Grouper()
    {
        const byte GT = (byte)'>';
        var startHeader = 0;
        var i = 1;
        var data = new List<page>();
        page page = null;

        while (!readQue.IsCompleted)
        {
            page = readQue.Take();
            data.Add(page);
            var bytes = page.data;
            var l = page.length;
            for (; i < l; i++)
            {
                if (bytes[i] == GT)
                {
                    groupQue.Add(new sequence { pages = data, startHeader = startHeader, endExclusive = i });
                    startHeader = i;
                    data = new List<page> { page };
                }
            }
            i = 0;
        }
        groupQue.Add(new sequence { pages = data, startHeader = startHeader, endExclusive = page.length });
        groupQue.CompleteAdding();
    }

    static byte map(byte b)
    {
        switch (b)
        {
            case (byte)'A': return (byte)'T';
            case (byte)'B': return (byte)'V';
            case (byte)'C': return (byte)'G';
            case (byte)'D': return (byte)'H';
            case (byte)'G': return (byte)'C';
            case (byte)'H': return (byte)'D';
            case (byte)'K': return (byte)'M';
            case (byte)'M': return (byte)'K';
            case (byte)'R': return (byte)'Y';
            case (byte)'T': return (byte)'A';
            case (byte)'V': return (byte)'B';
            case (byte)'Y': return (byte)'R';
            case (byte)'a': return (byte)'T';
            case (byte)'b': return (byte)'V';
            case (byte)'c': return (byte)'G';
            case (byte)'d': return (byte)'H';
            case (byte)'g': return (byte)'C';
            case (byte)'h': return (byte)'D';
            case (byte)'k': return (byte)'M';
            case (byte)'m': return (byte)'K';
            case (byte)'r': return (byte)'Y';
            case (byte)'t': return (byte)'A';
            case (byte)'v': return (byte)'B';
            case (byte)'y': return (byte)'R';
            default: return b;
        }
    }

    static void Reverser()
    {
        const byte LF = 10;

        while (!groupQue.IsCompleted)
        {
            var sequence = groupQue.Take();
            var startPageId = 0;
            var startPage = sequence.pages[0];
            var startBytes = startPage.data;
            var startLength = startPage.length;
            var startIndex = sequence.startHeader + 1;

            do
            {
                if (startIndex == startLength)
                {
                    startPage = sequence.pages[startPageId++];
                    startBytes = startPage.data;
                    startLength = startPage.length;
                    startIndex = 0;
                }
            } while (startBytes[startIndex++] != LF);


            var endPageId = sequence.pages.Count - 1;
            var endPage = sequence.pages[endPageId];
            var endBytes = endPage.data;
            var endIndex = sequence.endExclusive - 2;

            do
            {
                var startByte = startBytes[startIndex];
                if(startByte==LF)
                {
                    if (++startIndex == startLength)
                    {
                        startIndex = 0;
                        startPage = sequence.pages[startPageId++];
                        startBytes = startPage.data;
                        startLength = startPage.length;
                    }
                    if (startIndex == endIndex && startPageId == endPageId) break;
                    startByte = startBytes[startIndex];
                }
                var endByte = endBytes[endIndex];
                if(endByte==LF)
                {
                    if (--endIndex == -1)
                    {
                        endPage = sequence.pages[endPageId++];
                        endBytes = endPage.data;
                        endIndex = endPage.length - 1;
                    }
                    if (startIndex == endIndex && startPageId == endPageId) break;
                    endByte = endBytes[endIndex];
                }
                startBytes[startIndex] = map(endByte);
                endBytes[endIndex] = map(startByte);
                
                if (++startIndex == startLength)
                {
                    startIndex = 0;
                    startPage = sequence.pages[startPageId++];
                    startBytes = startPage.data;
                    startLength = startPage.length;
                }
                if (--endIndex == -1)
                {
                    endPage = sequence.pages[endPageId++];
                    endBytes = endPage.data;
                    endIndex = endPage.length - 1;
                }
            } while (startPageId < endPageId || (startPageId == endPageId && startIndex < endIndex));

            if (startIndex == endIndex) startBytes[startIndex] = map(startBytes[startIndex]);
            writeQue.Add(sequence);
        }
        writeQue.CompleteAdding();
    }

    static void Writer()
    {
        using (var stream = Console.OpenStandardOutput())
        {
            while (!writeQue.IsCompleted)
            {
                var sequence = writeQue.Take();
                var startIndex = sequence.startHeader;
                var pages = sequence.pages;

                for (int i = 0; i < pages.Count - 1; i++)
                {
                    var page = pages[i];
                    stream.Write(page.data, startIndex, page.length - startIndex);
                    returnBuffer(page.data);
                    startIndex = 0;
                }
                stream.Write(pages[pages.Count - 1].data, startIndex, sequence.endExclusive - startIndex);
            }
        }
    }

    public static void Main(string[] args)
    {
        Task.Run((Action)Reader);
        Task.Run((Action)Grouper);
        Task.Run((Action)Reverser);
        Task.Run((Action)Writer).Wait();
    }
}