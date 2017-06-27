/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 
   submitted by Josh Goldfoot
  
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class KNucleotidePage { public byte[] Data; public volatile KNucleotidePage NextPage; }

public static class KNucleotideImproved
{
    const int READER_BUFFER_SIZE = 1024 * 128;
    static ConcurrentBag<byte[]> bytePool = new ConcurrentBag<byte[]>(); 
    static BlockingCollection<KNucleotidePage> readQue = new BlockingCollection<KNucleotidePage>();
    static int blockThreeStart = -1;
    static volatile KNucleotidePage blockThree;

    static byte[] borrowBuffer()
    {
        byte[] ret;
        return bytePool.TryTake(out ret) ? ret : new byte[READER_BUFFER_SIZE];
    }

    static void returnBuffer(byte[] bytes)
    {
        if(bytes.Length==READER_BUFFER_SIZE) bytePool.Add(bytes);
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
                if(bytesRead != READER_BUFFER_SIZE) Array.Resize(ref buffer, bytesRead);
                readQue.Add(new KNucleotidePage { Data = buffer });
            }
            readQue.CompleteAdding();
        }
    }

    static KNucleotidePage NextPage()
    {
        KNucleotidePage page = null;
        while(!readQue.IsCompleted && !readQue.TryTake(out page)) Thread.SpinWait(0);
        return page;
    }

    static void Locator()
    {
        const byte GT = (byte)'>';
        var toMatch = new byte[] { GT, (byte)'T', (byte)'H', (byte)'R', (byte)'E', (byte)'E'};
        int i = 0, j = 0;
        var page = NextPage();
        for(;;)
        {
            if(page.Data[i]==toMatch[j])
            {
                if(j==5) break;
                j++;
            }
            else
            {
                j=0;
            }
            if(++i==page.Length)
            {
                if(j==0 && !readQue.IsCompleted)
                    returnBuffer(page.Data);
                page = NextPage();
                i = 0;
            }
        }
        do
        {
            if(++i==page.Length)
            {
                page = NextPage();
                i = 0;
            }
        } while (page.Data[i]!='\n');
        blockThreeStart = i+1;
        var lastPage = blockThree = page;
        do
        {
            if(++i==page.Length)
            {
                page = NextPage();
                if(page==null)
                {
                    lastPage.NextPage = new KNucleotidePage { Length = 0 };
                    break;
                }
                else
                {
                    lastPage.NextPage = page;
                    lastPage = page;
                }
                i = 0;
            }
        } while(page.Data[i]!=GT);
    }

    public static void Main(string[] args)
    {   
        new Thread(Reader).Start();
        var locator = new Thread(Locator);
        locator.Start();
        PrepareLookups();
        var lengths = new int[]{1,2,3,4,6,12,18};
        var lookups = new string[]{null,null,"GGT","GGTA","GGTATT","GGTATTTTAATT","GGTATTTTAATTTATAGT"};
        var results = new StringBuilder[7];
        for(int i=0; i<results.Length; i++) results[i] = new StringBuilder();
        //while(blockThree==null) Thread.SpinWait(0);
        locator.Join();
        int buflen = -1;
        Parallel.For(0, 7, i =>
        {
            var l = lengths[i];
            var p = lookups[i];
            var dict = CountFrequency(l);
            if(p==null)
            {
                if(i==0) buflen = dict.Values.Sum(w => w.v);
                WriteFrequencies(results[i], dict, buflen, l);
            }
            else
                WriteCount(results[i], dict, p);     
        });
        foreach(var sb in results) Console.Write(sb);
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

    private static Dictionary<ulong, Wrapper2> CountFrequency(int fragmentLength)
    {
        var dictionary = new Dictionary<ulong, Wrapper2>();
        var page = blockThree;
        int i = blockThreeStart;

        ulong rollingKey = 0;
        ulong mask = 0;
        
        for (int cursor = 0; cursor < fragmentLength - 1; cursor++)
        {
            rollingKey <<= 2;
            rollingKey |= tonum[page.Data[i]];
            mask = (mask << 2) + 3;
            
            if(++i==page.Length)
            {
                while(page.NextPage==null) Thread.Sleep(0);
                page = page.NextPage;
                i = 0;
            }
        }

        mask = (mask << 2) + 3;
        Wrapper2 w;
        const byte a = (byte)'a';
        const byte GT = (byte)'>';
        for(;;)
        {
            var cursorByte = page.Data[i];
            if(cursorByte >= a)
            {
                rollingKey = ((rollingKey << 2) & mask) | tonum[cursorByte];
                if (dictionary.TryGetValue(rollingKey, out w))
                    w.v++;
                else
                    dictionary.Add(rollingKey, new Wrapper2 { v = 1 });
            }
            else if(cursorByte==GT)
            {
                break;
            }

            if(++i==page.Length)
            {
                while(page.NextPage==null) Thread.Sleep(0);
                page = page.NextPage;
                if(page.Length==0) break;
                i = 0;
            }
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