/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 
   submitted by Josh Goldfoot
  
 */

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

class WrapperImproved { public int v; }
public static class KNucleotideImproved
{
    const int READER_BUFFER_SIZE = 1024 * 128, LARGEST_SEQUENCE = 250000000;
    static volatile int threeStart = -1, threeEnd = -1, threeBlockLastId;
    static byte[][] threeBlocks = new byte[LARGEST_SEQUENCE/READER_BUFFER_SIZE+3][];
    const int COUNT_LENGTH = 7;
    static int[] countKeyLengths = new []{18,12,6,4,3,2,1};
    static Dictionary<ulong,WrapperImproved>[] countDictionary = new Dictionary<ulong,WrapperImproved>[COUNT_LENGTH];
    static int[] countBlockWorking = new int[COUNT_LENGTH];
    static ulong[] countRollingKey = new ulong[COUNT_LENGTH];
    static ulong[] countMask = new ulong[COUNT_LENGTH];
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

    static void process()
    {
        const byte A = (byte)'a';
        while(true)
        {
            bool finished = threeEnd>=0;
            int countId=0,working=0,lastId=threeBlockLastId;
            for(; countId<COUNT_LENGTH; countId++)
            {
                working = countBlockWorking[countId];
                if(working>=0 && working<=lastId
                && Interlocked.CompareExchange(ref countBlockWorking[countId], -1, working)==working) break;
            }
            
            if(countId==COUNT_LENGTH)
            {
                if(finished) break; 
                else continue;
            }
            
            // work block working
            if(working==0)
            {
                var dictionary = countDictionary[countId] = new Dictionary<ulong,WrapperImproved>();
                int i = threeStart;

                ulong rollingKey = 0;
                ulong mask = 0;
                var block = threeBlocks[working];
                var cursorEnd = countKeyLengths[countId]-1;
                for (int cursor = 0; cursor<cursorEnd; cursor++)
                {
                    rollingKey <<= 2;
                    rollingKey |= tonum[block[i]];
                    mask = (mask << 2) + 3;
                    
                    if(++i==READER_BUFFER_SIZE)
                    {
                        while(threeBlocks[1]==null) Thread.Sleep(0);
                        block = threeBlocks[1];
                        working = 1;
                        i = 0;
                    }
                }
                mask = (mask << 2) + 3;
                countMask[countId] = mask;

                for(;i<block.Length;i++)
                {
                    var cursorByte = block[i];
                    if(cursorByte>=A)
                    {
                        rollingKey = ((rollingKey << 2) & mask) | tonum[cursorByte];
                        WrapperImproved w;
                        if (dictionary.TryGetValue(rollingKey, out w))
                            w.v++;
                        else
                            dictionary.Add(rollingKey, new WrapperImproved { v = 1 });
                    }
                    else if(cursorByte==0)
                    {
                        break;
                    }
                }
                countRollingKey[countId] = rollingKey;
            }
            else
            {
                var dictionary = countDictionary[countId];
                var rollingKey = countRollingKey[countId];
                var mask = countMask[countId];
                foreach(var cursorByte in threeBlocks[working])
                {
                    if(cursorByte>=A)
                    {
                        rollingKey = ((rollingKey << 2) & mask) | tonum[cursorByte];
                        WrapperImproved w;
                        if (dictionary.TryGetValue(rollingKey, out w))
                            w.v++;
                        else
                            dictionary.Add(rollingKey, new WrapperImproved { v = 1 });
                    }
                    else if(cursorByte==0)
                    {
                        break;
                    }
                }
                countRollingKey[countId] = rollingKey;
            }

            // end block working

            countBlockWorking[countId] = working+1;
        }
    }

    public static void Main(string[] args)
    {
        PrepareLookups();
        var threads = new Thread[Environment.ProcessorCount-1];
        for(int i=0; i<threads.Length; i++) threads[i] = new Thread(process);

        using (var stream = File.OpenRead(@"C:\temp\input25000000.txt"))//Console.OpenStandardInput())
        {
            // find three sequence
            int matchIndex = 0;
            var toFind = new [] {(byte)'>', (byte)'T', (byte)'H', (byte)'R', (byte)'E', (byte)'E'};
            var buffer = new byte[READER_BUFFER_SIZE];
            do
            {
                read(stream, buffer, 0, READER_BUFFER_SIZE);
                threeStart = find(buffer, toFind, 0, ref matchIndex);
            } while (threeStart==-1);
            
            // Skip to end of line
            matchIndex = 0;
            toFind = new [] {(byte)'\n'};
            threeStart = find(buffer, toFind, threeStart, ref matchIndex);
            while(threeStart==-1)
            {
                read(stream, buffer, 0, READER_BUFFER_SIZE);
                threeStart = find(buffer, toFind, 0, ref matchIndex);
            }
            threeBlocks[0] = buffer;
            
            // something to work on now 
            foreach(var thread in threads) thread.Start();

            // find next seq or end of input
            matchIndex = 0;
            toFind = new [] {(byte)'>'};
            threeEnd = find(buffer, toFind, threeStart, ref matchIndex);
            
            while(threeEnd==-1)
            {
                buffer = new byte[READER_BUFFER_SIZE];
                var bytesRead = read(stream, buffer, 0, READER_BUFFER_SIZE);
                threeEnd =  bytesRead==READER_BUFFER_SIZE ? find(buffer, toFind, 0, ref matchIndex) : bytesRead;
                threeBlocks[threeBlockLastId+1] = buffer;
                threeBlockLastId++;
            }
        }

        process();
        foreach(var thread in threads) thread.Join();
            
        var dict6 = countDictionary[6];
        int buflen = 0;
        foreach(var w in dict6.Values) buflen += w.v;
        WriteFrequencies(dict6, buflen, countKeyLengths[6]);
        WriteFrequencies(countDictionary[5], buflen, countKeyLengths[5]);
        WriteCount(countDictionary[4], "GGT");
        WriteCount(countDictionary[3], "GGTA");
        WriteCount(countDictionary[2], "GGTATT");
        WriteCount(countDictionary[1], "GGTATTTTAATT");
        WriteCount(countDictionary[0], "GGTATTTTAATTTATAGT");
    }

    static void WriteFrequencies(Dictionary<ulong, WrapperImproved> freq, int buflen, int fragmentLength)
    {
        var sb = new StringBuilder();
        double percent = 100.0 / (buflen - fragmentLength + 1);
        foreach(var kv in freq.OrderByDescending(i => i.Value.v))
        {       
            sb.Append(PrintKey(kv.Key, fragmentLength));
            sb.Append(" ");
            sb.AppendLine((kv.Value.v * percent).ToString("F3"));
        }
        Console.Out.WriteLine(sb.ToString());
    }

    static void WriteCount(Dictionary<ulong, WrapperImproved> dictionary, string fragment)
    {
        ulong key = 0;
        var keybytes = Encoding.ASCII.GetBytes(fragment.ToLower());
        for (int i = 0; i < keybytes.Length; i++)
        {
            key <<= 2;
            key |= tonum[keybytes[i]];
        }
        WrapperImproved w;
        var n = dictionary.TryGetValue(key, out w) ? w.v : 0;
        Console.Out.WriteLine(n+"\t"+fragment);
    }

    static string PrintKey(ulong key, int fragmentLength)
    {
        char[] items = new char[fragmentLength];
        for (int i = 0; i < fragmentLength; ++i)
        {
            items[fragmentLength - i - 1] = tochar[key & 0x3];
            key >>= 2;
        }
        return new string(items);
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