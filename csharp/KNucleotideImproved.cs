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
using System.Runtime.CompilerServices;

class WrapperImproved { public int v=1; }
public static class KNucleotideImproved
{
    const int BLOCK_SIZE = 1024 * 1024 * 8;
    static List<byte[]> threeBlocks = new List<byte[]>();
    static int threeStart, threeEnd;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Increment(this Dictionary<long, WrapperImproved> dict, long rollingKey)
    {
        WrapperImproved w;
        if (dict.TryGetValue(rollingKey, out w))
            w.v++;
        else
            dict[rollingKey] = new WrapperImproved();
    }

    static Dictionary<long,WrapperImproved> countEnding(int l, long mask, byte b)
    {
        long rollingKey = 0;
        var firstBlock = threeBlocks[0];
        while(--l>0) rollingKey = (rollingKey<<2) | firstBlock[threeStart++];
        var dict = new Dictionary<long,WrapperImproved>();
        for(int i=threeStart; i<firstBlock.Length; i++)
        {
            var nb = firstBlock[i];
            if(nb==b) dict.Increment(rollingKey);
            else if(nb==255) continue;
            rollingKey = ((rollingKey << 2) | nb) & mask;
        }
        int lastBlockId = threeBlocks.Count-1; 
        for(int bl=1; bl<lastBlockId; bl++)
        {
            var bytes = threeBlocks[bl];
            for(int i=0; i<bytes.Length; i++)
            {
                var nb = bytes[i];
                if(nb==b) dict.Increment(rollingKey);
                else if(nb==255) continue;
                rollingKey = ((rollingKey << 2) | nb) & mask;
            }
        }
        var lastBlock = threeBlocks[lastBlockId];
        for(int i=0; i<threeEnd; i++)
        {
            var nb = lastBlock[i];
            if(nb==b) dict.Increment(rollingKey);
            else if(nb==255) continue;
            rollingKey = ((rollingKey << 2) | nb) & mask;
        }
        return dict;
    }

    static Task<string> count(int l, long mask, Func<Dictionary<long,int>,string> summary)
    {
        return Task.Factory.ContinueWhenAll(
            new [] {
                Task.Run(() => countEnding(l, mask, 0)),
                Task.Run(() => countEnding(l, mask, 1)),
                Task.Run(() => countEnding(l, mask, 2)),
                Task.Run(() => countEnding(l, mask, 3))
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

        using (var stream = File.OpenRead(@"C:\temp\input25000000.txt")/*Console.OpenStandardInput()*/)
        {
            // find three sequence
            int matchIndex = 0;
            var toFind = new [] {(byte)'>', (byte)'T', (byte)'H', (byte)'R', (byte)'E', (byte)'E'};
            var buffer = new byte[BLOCK_SIZE];
            do
            {
                read(stream, buffer, 0, BLOCK_SIZE);
                threeStart = find(buffer, toFind, 0, ref matchIndex);
            } while (threeStart==-1);
            
            // Skip to end of line
            matchIndex = 0;
            toFind = new [] {(byte)'\n'};
            threeStart = find(buffer, toFind, threeStart, ref matchIndex);
            while(threeStart==-1)
            {
                read(stream, buffer, 0, BLOCK_SIZE);
                threeStart = find(buffer, toFind, 0, ref matchIndex);
            }
            threeBlocks.Add(buffer);
            
            // find next seq or end of input
            matchIndex = 0;
            toFind = new [] {(byte)'>'};
            threeEnd = find(buffer, toFind, threeStart, ref matchIndex);
            while(threeEnd==-1)
            {
                buffer = new byte[BLOCK_SIZE];
                var bytesRead = read(stream, buffer, 0, BLOCK_SIZE);
                threeEnd = bytesRead==BLOCK_SIZE ? find(buffer, toFind, 0, ref matchIndex)
                         : bytesRead;
                threeBlocks.Add(buffer);
            }
        }

        if(threeBlocks.Count==1) 
        { // Need to be at least 2 blocks
            var bytes = threeBlocks[0];
            for(int i=threeEnd+1; i<bytes.Length; i++)
                bytes[i] = 255;
            threeEnd = 0;
            threeBlocks.Add(new byte[]{255});
        }
        else if(threeStart+18>BLOCK_SIZE)
        { // Key needs to be in first block
            byte[] block0 = threeBlocks[0], block1 = threeBlocks[1];
            Buffer.BlockCopy(block0, threeStart, block0, threeStart-18, BLOCK_SIZE-threeStart);
            Buffer.BlockCopy(block1, 0, block0, BLOCK_SIZE-18, 18);
            for(int i=0; i<18; i++) block1[i] = 255;
        }
        
        Parallel.ForEach(threeBlocks, bytes =>
        {
            for(int i=0; i<bytes.Length; i++)
                bytes[i] = tonum[bytes[i]];
        });

        var taskString18 = count(18, 68719476736/2-1, // 4**17-1
            d => writeCount(d, "GGTATTTTAATTTATAGT"));
        
        var taskString12 = count(12, 16777216/2-1, // 4**11-1
            d => writeCount(d, "GGTATTTTAATT"));        
        
        var taskString6 = count(6, 1023, // 4**5-1
            d => writeCount(d, "GGTATT"));
        
        var taskString4 = count(4, 63, // 4**3-1
            d => writeCount(d, "GGTA"));
        
        var taskString3 = count(3, 15, // 4**2-1
            d => writeCount(d, "GGT"));

        var taskString2 = count(2, 3, // 4**1-1
            d => writeFrequencies(d, 2));

        var taskString1 = count(1, 0, // 4**0-1
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