/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 *
 * submitted by Josh Goldfoot
 * 
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class KNucleotideLinkedPage { public byte[] data; public int length; public KNucleotideLinkedPage next; }

public static class KNucleotideImproved
{
    const int READER_BUFFER_SIZE = 1024 * 128;
    static BlockingCollection<page> readQue = new BlockingCollection<page>();
    static ConcurrentBag<byte[]> bytePool = new ConcurrentBag<byte[]>(); 

    static byte[] borrowBuffer()
    {
        byte[] ret;
        return bytePool.TryTake(out ret) ? ret : new byte[READER_BUFFER_SIZE];
    }

    static void returnBuffer(byte[] bytes)
    {
        bytePool.Add(bytes);
    }

    static void Reader()
    {
        using (var stream = File.OpenRead(@"C:\temp\input25000000.txt"))//Console.OpenStandardInput())
        {
            for (;;)
            {
                var buffer = borrowBuffer();
                var bytesRead = stream.Read(buffer, 0, READER_BUFFER_SIZE);
                if (bytesRead == 0) break;
                readQue.Add(new page { data = buffer, length = bytesRead });
            }
            readQue.CompleteAdding();
        }
    }

    static bool tryTake<T>(BlockingCollection<T> q, out T t) where T : class
    {
        t = null;
        while(!q.IsCompleted && !q.TryTake(out t)) Thread.SpinWait(0);
        return t!=null;
    }

    static void Locator()
    {
        
    }

    public static void Main(string[] args)
    {   
        Task.Run((Action)Reader);
        Task.Run((Action)Locator);
        PrepareLookups();
        var task1 = Task.Factory.StartNew(() => CountFrequency(buffer, 1));
        var task2 = Task.Factory.StartNew(() => CountFrequency(buffer, 2));
        var task3 = Task.Factory.StartNew(() => CountFrequency(buffer, 3));
        var task4 = Task.Factory.StartNew(() => CountFrequency(buffer, 4));
        var task6 = Task.Factory.StartNew(() => CountFrequency(buffer, 6));
        var task12 = Task.Factory.StartNew(() => CountFrequency(buffer, 12));
        var task18 = Task.Factory.StartNew(() => CountFrequency(buffer, 18));
        var sb = new StringBuilder();
        int buflen = task1.Result.Values.Sum(i => i.v);
        WriteFrequencies(sb, task1.Result, buflen, 1);
        WriteFrequencies(sb, task2.Result, buflen, 2);
        WriteCount(sb, task3.Result, "GGT");
        WriteCount(sb, task4.Result, "GGTA");
        WriteCount(sb, task6.Result, "GGTATT");
        WriteCount(sb, task12.Result, "GGTATTTTAATT");
        WriteCount(sb, task18.Result, "GGTATTTTAATTTATAGT");
        Console.Write(sb);
    }

    private static void WriteFrequencies(StringBuilder sb, Dictionary<ulong, Wrapper2> freq, int buflen, int fragmentLength)
    {
        double percent = 100.0 / (buflen - fragmentLength + 1);
        foreach(var kv in freq.OrderByDescending(i => i.Value.v))
        {
            
            sb.Append(PrintKey(kv.Key, fragmentLength));
            sb.Append(" ");
            sb.AppendLine((kv.Value.v * percent).ToString("f3"));
        }
        sb.AppendLine();
    }

    private static void WriteCount(StringBuilder sb, Dictionary<ulong, Wrapper2> dictionary, string fragment)
    {
        ulong key = 0;
        var keybytes = Encoding.ASCII.GetBytes(fragment.ToLower());
        for (int i = 0; i < keybytes.Length; i++)
        {
            key <<= 2;
            key |= tonum[keybytes[i]];
        }
        Wrapper2 w;
        sb.Append(dictionary.TryGetValue(key, out w) ? w.v : 0);
        sb.Append("\t");
        sb.AppendLine(fragment);
    }

    private static string PrintKey(ulong key, int fragmentLength)
    {
        char[] items = new char[fragmentLength];
        for (int i = 0; i < fragmentLength; ++i)
        {
            items[fragmentLength - i - 1] = tochar[key & 0x3];
            key >>= 2;
        }
        return new string(items);
    }

    private static Dictionary<ulong, Wrapper2> CountFrequency(byte[] buffer, int fragmentLength)
    {
        var dictionary = new Dictionary<ulong, Wrapper2>();
        ulong rollingKey = 0;
        ulong mask = 0;
        int cursor;
        for (cursor = 0; cursor < fragmentLength - 1; cursor++)
        {
            rollingKey <<= 2;
            rollingKey |= tonum[buffer[cursor]];
            mask = (mask << 2) + 3;
        }
        mask = (mask << 2) + 3;
        int stop = buffer.Length;
        Wrapper2 w;
        byte cursorByte;
        while (cursor < stop)
        {
            if ((cursorByte = buffer[cursor++]) < (byte)'a')
                cursorByte = buffer[cursor++];
            rollingKey = ((rollingKey << 2) & mask) | tonum[cursorByte];
            if (dictionary.TryGetValue(rollingKey, out w))
                w.v++;
            else
                dictionary.Add(rollingKey, new Wrapper2 { v = 1 });
        }
        return dictionary;
    }

    private static byte[] tonum = new byte[256];
    private static char[] tochar = new char[4];
    private static void PrepareLookups()
    {
        tonum['a'] = 0;
        tonum['c'] = 1;
        tonum['g'] = 2;
        tonum['t'] = 3;
        tochar[0] = 'A';
        tochar[1] = 'C';
        tochar[2] = 'G';
        tochar[3] = 'T';
    }
}

class Wrapper2 { public int v; }