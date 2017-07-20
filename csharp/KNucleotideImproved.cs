/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 
   submitted by Josh Goldfoot
  
 */

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class WrapperImproved { public int v=1; }
public static class KNucleotideImproved
{
    const int READER_BUFFER_SIZE = 1024 * 1024 * 8;
    static byte[] tonum = new byte[256];
    static char[] tochar = new char[] {'A', 'C', 'G', 'T'};

    static string writeFrequencies(Dictionary<long,int> freq, int fragmentLength)
    {
        var sb = new StringBuilder();
        double percent = 100.0 / freq.Values.Sum();
        foreach(var kv in freq.OrderByDescending(i => i.Value))
        {
            var keyChars = new char[fragmentLength];
            var key = kv.Key;
            for (int i=keyChars.Length-1; i>=0; --i)
            {
                keyChars[i] = tochar[key & 0x3];
                key >>= 2;
            }
            sb.Append(keyChars);   
            sb.Append(" ");
            sb.AppendLine((kv.Value * percent).ToString("F3"));
        }
        return sb.ToString();
    }

    static string writeCount(Dictionary<long,int> dictionary, string fragment)
    {
        long key = 0;
        for (int i=0; i<fragment.Length; ++i)
        {
            key = (key << 2) | tonum[fragment[i]];
        }
        int w;
        var n = dictionary.TryGetValue(key, out w) ? w : 0;
        return n+"\t"+fragment;
    }

    static Dictionary<long,WrapperImproved> calcDictionary(List<byte[]> blocks, int threeStart, int threeEnd, int l, long mask, byte b)
    {
        long rollingKey = 0;
        var firstBlock = blocks[0];
        while(--l>0) rollingKey = (rollingKey<<2) | firstBlock[threeStart++];
        var dict = new Dictionary<long,WrapperImproved>();
        for(int i=threeStart; i<firstBlock.Length; i++)
        {
            var nb = firstBlock[i];
            if(nb==b)
            {
                WrapperImproved w;
                if (dict.TryGetValue(rollingKey, out w))
                    w.v++;
                else
                    dict[rollingKey] = new WrapperImproved();
            }
            else if(nb==255) continue;
            rollingKey = ((rollingKey << 2) | nb) & mask;
        }
        for(int bl=1; bl<blocks.Count-1; bl++)
        {
            var bytes = blocks[bl];
            for(int i=0; i<bytes.Length; i++)
            {
                var nb = bytes[i];
                if(nb==b)
                {
                    WrapperImproved w;
                    if (dict.TryGetValue(rollingKey, out w))
                        w.v++;
                    else
                        dict[rollingKey] = new WrapperImproved();
                }
                else if(nb==255) continue;
                rollingKey = ((rollingKey << 2) | nb) & mask;
            }
        }
        var lastBlock = blocks[blocks.Count-1];
        for(int i=0; i<=threeEnd; i++)
        {
            var nb = lastBlock[i];
            if(nb==b)
            {
                WrapperImproved w;
                if (dict.TryGetValue(rollingKey, out w))
                    w.v++;
                else
                    dict[rollingKey] = new WrapperImproved();
            }
            else if(nb==255) continue;
            rollingKey = ((rollingKey << 2) | nb) & mask;
        }
        return dict;
    }

    static Task<string> count(List<byte[]> blocks, int threeStart, int threeEnd, int l, long mask, Func<Dictionary<long,int>,string> summary)
    {
        return Task.Factory.ContinueWhenAll(
            new [] {
                Task.Run(() => calcDictionary(blocks, threeStart, threeEnd, l, mask, 0)),
                Task.Run(() => calcDictionary(blocks, threeStart, threeEnd, l, mask, 1)),
                Task.Run(() => calcDictionary(blocks, threeStart, threeEnd, l, mask, 2)),
                Task.Run(() => calcDictionary(blocks, threeStart, threeEnd, l, mask, 3))
            }
            , dicts => {
                var d = new Dictionary<long,int>(dicts.Sum(i => i.Result.Count));
                for(byte i=0; i<dicts.Length; i++)
                    foreach(var kv in dicts[i].Result)
                        d[(kv.Key << 2) | i] = kv.Value.v;
                return summary(d);
            });
    }

    static int read(Stream stream, byte[] buffer, int offset, int count)
    {
        var bytesRead = stream.Read(buffer, offset, count);
        return bytesRead==count ? offset+count
             : bytesRead==0 ? offset
             : read(stream, buffer, offset+bytesRead, count-bytesRead);
    }
    
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

    public static void Main(string[] args)
    {
        tonum['\n'] = 255; tonum['>'] = 255;
        tonum['c'] = 1; tonum['C'] = 1;
        tonum['g'] = 2; tonum['G'] = 2;
        tonum['t'] = 3; tonum['T'] = 3;

        var threeBlocks = new List<byte[]>();
        int threeStart = 0, threeEnd = 0;
        using (var stream = File.OpenRead(@"C:\temp\input25000000.txt")/*Console.OpenStandardInput()*/)
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
            threeBlocks.Add(buffer);
            
            // find next seq or end of input
            matchIndex = 0;
            toFind = new [] {(byte)'>'};
            threeEnd = find(buffer, toFind, threeStart, ref matchIndex);
            while(threeEnd==-1)
            {
                buffer = new byte[READER_BUFFER_SIZE];
                var bytesRead = read(stream, buffer, 0, READER_BUFFER_SIZE);
                threeEnd = bytesRead==READER_BUFFER_SIZE ? find(buffer, toFind, 0, ref matchIndex) : bytesRead;
                threeBlocks.Add(buffer);
            }
        }

        threeStart++;

        // TODO: correct if no space for key in first block
        // TODO: correct if only 1 or 2 blocks
        // TODO: maybe only do whats needed in tonum
        // Console.WriteLine(threeBlocks.Count);
        // Console.WriteLine(threeBlocks[0].Length);
        // Console.WriteLine(threeBlocks[1].Length);
        // Console.WriteLine(threeBlocks.Last().Length);

        var tasks = new Task[Environment.ProcessorCount];
        int step = (threeBlocks.Count-1)/tasks.Length+1;
        for(int t=0; t<tasks.Length; ++t)
        {
            int start = t*step;
            int end = Math.Min(start+step, threeBlocks.Count);
            tasks[t] = Task.Run(() =>
            {
                for(int b=start; b<end; ++b)
                {
                    var bytes = threeBlocks[b];
                    for(int i=0; i<bytes.Length; i++)
                        bytes[i] = tonum[bytes[i]];
                }
            });
        }

        Task.WaitAll(tasks);

        var taskString18 = count(threeBlocks, threeStart, threeEnd, 18, 68719476736/2-1,//4**17-1
            d => writeCount(d, "GGTATTTTAATTTATAGT"));
        
        var taskString12 = count(threeBlocks, threeStart, threeEnd, 12, 16777216/2-1,//4**11-1
            d => writeCount(d, "GGTATTTTAATT"));        
        
        var taskString6 = count(threeBlocks, threeStart, threeEnd, 6, 1023,//4**5-1
            d => writeCount(d, "GGTATT"));
        
        var taskString4 = count(threeBlocks, threeStart, threeEnd, 4, 63,//4**3-1
            d => writeCount(d, "GGTA"));
        
        var taskString3 = count(threeBlocks, threeStart, threeEnd, 3, 15,//4**2-1
            d => writeCount(d, "GGT"));

        var taskString2 = count(threeBlocks, threeStart, threeEnd, 2, 3,//4**1-1
            d => writeFrequencies(d, 2));

        var taskString1 = count(threeBlocks, threeStart, threeEnd, 1, 0,//4**0-1
            d => writeFrequencies(d, 1));

        Console.Out.WriteLineAsync(taskString1.Result);
        Console.Out.WriteLineAsync(taskString2.Result);
        Console.Out.WriteLineAsync(taskString3.Result);
        Console.Out.WriteLineAsync(taskString4.Result);
        Console.Out.WriteLineAsync(taskString6.Result);
        Console.Out.WriteLineAsync(taskString12.Result);
        Console.Out.WriteLineAsync(taskString18.Result);
    }
}