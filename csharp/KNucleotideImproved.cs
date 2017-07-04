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

class WrapperImproved { public int v; }
public static class KNucleotideImproved
{
    const int READER_BUFFER_SIZE = 1024 * 128;
    const byte GT = (byte)'>';
    static int threeStart = -1, threeEnd = -1;
    static LinkedList<byte[]> threeBlocks = new LinkedList<byte[]>();
    
    static int find(byte[] buffer, byte[] toFind, int i, ref int matchIndex)
    {
        if(matchIndex==0)
        {
            i = Array.IndexOf(buffer, toFind[0], i);
            if(i==-1) return -1;
            matchIndex = 1;
            return find(buffer, toFind, i+1, ref matchIndex);
        }
        else
        {
            while(i<buffer.Length && matchIndex<toFind.Length)
            {
                if(buffer[i++]!=toFind[matchIndex++])
                {
                    matchIndex = 0;
                    return find(buffer, toFind, i, ref matchIndex);
                }
            }
            return matchIndex==toFind.Length ? i-1 : -1;
        }
    }

    static int read(Stream stream, byte[] buffer, int offset, int count)
    {
        var bytesRead = stream.Read(buffer, offset, count);
        return bytesRead==count ? offset+count
             : bytesRead==0 ? offset
             : read(stream, buffer, offset+bytesRead, count-bytesRead);
    }
    
    static void Reader()
    {
        using (var stream = File.OpenRead(@"C:\temp\input25000000.txt"))//Console.OpenStandardInput())
        {
            // find three sequence
            int matchIndex = 0;
            var toFind = new [] {GT, (byte)'T', (byte)'H', (byte)'R', (byte)'E', (byte)'E'};
            var buffer = new byte[READER_BUFFER_SIZE];
            do
            {
                stream.Read(buffer, 0, READER_BUFFER_SIZE);
                threeStart = find(buffer, toFind, 0, ref matchIndex);
            } while (threeStart==-1);
            
            // Skip to end of line
            matchIndex = 0;
            toFind = new [] {(byte)'\n'};
            threeStart = find(buffer, toFind, threeStart, ref matchIndex);
            while(threeStart==-1)
            {
                stream.Read(buffer, 0, READER_BUFFER_SIZE);
                threeStart = find(buffer, toFind, 0, ref matchIndex);
            }
            threeBlocks.AddLast(buffer);

            // find next seq or end of input
            matchIndex = 0;
            toFind = new [] {GT};
            threeEnd = find(buffer, toFind, threeStart, ref matchIndex);
            while(threeEnd==-1)
            {
                buffer = new byte[READER_BUFFER_SIZE];
                var bytesRead = read(stream, buffer, 0, READER_BUFFER_SIZE);
                threeBlocks.AddLast(buffer);
                threeEnd = bytesRead==READER_BUFFER_SIZE ? find(buffer, toFind, 0, ref matchIndex) : bytesRead;
            }
        }
    }

    // static byte[] NextPage()
    // {
    //     byte[] bytes = null;
    //     while(!readQue.IsCompleted && !readQue.TryTake(out bytes)) Thread.SpinWait(0);
    //     return bytes;
    // }


    public static void Main(string[] args)
    {   
        Reader();

        Console.WriteLine("start: " + threeStart);
        Console.WriteLine("end: " + threeEnd);
        Console.WriteLine("block length: " + threeBlocks.Count);

        // new Thread(Reader).Start();
        // var locator = new Thread(Locator);
        // locator.Start();
        // PrepareLookups();
        // var lengths = new int[]{1,2,3,4,6,12,18};
        // var lookups = new string[]{null,null,"GGT","GGTA","GGTATT","GGTATTTTAATT","GGTATTTTAATTTATAGT"};
        // var results = new StringBuilder[7];
        // for(int i=0; i<results.Length; i++) results[i] = new StringBuilder();
        // //while(blockThree==null) Thread.SpinWait(0);
        // locator.Join();
        // int buflen = -1;
        // Parallel.For(0, 7, i =>
        // {
        //     var l = lengths[i];
        //     var p = lookups[i];
        //     var dict = CountFrequency(l);
        //     if(p==null)
        //     {
        //         if(i==0) buflen = dict.Values.Sum(w => w.v);
        //         WriteFrequencies(results[i], dict, buflen, l);
        //     }
        //     else
        //         WriteCount(results[i], dict, p);     
        // });
        // foreach(var sb in results) Console.Write(sb);
    }

    private static void WriteFrequencies(StringBuilder sb, Dictionary<ulong, WrapperImproved> freq, int buflen, int fragmentLength)
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

    private static void WriteCount(StringBuilder sb, Dictionary<ulong, WrapperImproved> dictionary, string fragment)
    {
        ulong key = 0;
        var keybytes = Encoding.ASCII.GetBytes(fragment.ToLower());
        for (int i = 0; i < keybytes.Length; i++)
        {
            key <<= 2;
            key |= tonum[keybytes[i]];
        }
        WrapperImproved w;
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

    // private static Dictionary<ulong, WrapperImproved> CountFrequency(int fragmentLength)
    // {
    //     var dictionary = new Dictionary<ulong, WrapperImproved>();
    //     var page = blockThree;
    //     int i = blockThreeStart;

    //     ulong rollingKey = 0;
    //     ulong mask = 0;
        
    //     for (int cursor = 0; cursor < fragmentLength - 1; cursor++)
    //     {
    //         rollingKey <<= 2;
    //         rollingKey |= tonum[page.Data[i]];
    //         mask = (mask << 2) + 3;
            
    //         if(++i==page.Length)
    //         {
    //             while(page.NextPage==null) Thread.Sleep(0);
    //             page = page.NextPage;
    //             i = 0;
    //         }
    //     }

    //     mask = (mask << 2) + 3;
    //     WrapperImproved w;
    //     const byte a = (byte)'a';
    //     const byte GT = (byte)'>';
    //     for(;;)
    //     {
    //         var cursorByte = page.Data[i];
    //         if(cursorByte >= a)
    //         {
    //             rollingKey = ((rollingKey << 2) & mask) | tonum[cursorByte];
    //             if (dictionary.TryGetValue(rollingKey, out w))
    //                 w.v++;
    //             else
    //                 dictionary.Add(rollingKey, new WrapperImproved { v = 1 });
    //         }
    //         else if(cursorByte==GT)
    //         {
    //             break;
    //         }

    //         if(++i==page.Length)
    //         {
    //             while(page.NextPage==null) Thread.Sleep(0);
    //             page = page.NextPage;
    //             if(page.Length==0) break;
    //             i = 0;
    //         }
    //     }
    //     return dictionary;
    // }

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