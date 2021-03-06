/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 
   submitted by Josh Goldfoot
   Modified to reduce memory and do more in parallel by Anthony Lloyd
 */

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

class Wrapper { public int v=1; }
public static class KNucleotide
{
    const int BLOCK_SIZE = 1024 * 1024 * 8;
    public static List<byte[]> threeBlocks = new List<byte[]>();
    public static int threeStart, threeEnd;
    static byte[] tonum = new byte[256];
    static char[] tochar = new char[] {'A', 'C', 'G', 'T'};

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
            int bl = buffer.Length, fl = toFind.Length;
            while(i<bl && matchIndex<fl)
            {
                if(buffer[i++]!=toFind[matchIndex++])
                {
                    matchIndex = 0;
                    return find(buffer, toFind, i, ref matchIndex);
                }
            }
            return matchIndex==fl ? i : -1;
        }
    }

    public static void LoadThreeData()
    {
        //var stream = Console.OpenStandardInput();
        var stream = System.IO.File.OpenRead(@"C:\Users\Ant\Google Drive\BenchmarkGame\fasta25000000.txt");
        
        // find three sequence
        int matchIndex = 0;
        var toFind = new [] {(byte)'>', (byte)'T', (byte)'H', (byte)'R', (byte)'E', (byte)'E'};
        var buffer = new byte[BLOCK_SIZE];
        do
        {
            threeEnd = read(stream, buffer, 0, BLOCK_SIZE);
            threeStart = find(buffer, toFind, 0, ref matchIndex);
        } while (threeStart==-1);
        
        // Skip to end of line
        matchIndex = 0;
        toFind = new [] {(byte)'\n'};
        threeStart = find(buffer, toFind, threeStart, ref matchIndex);
        while(threeStart==-1)
        {
            threeEnd = read(stream, buffer, 0, BLOCK_SIZE);
            threeStart = find(buffer, toFind, 0, ref matchIndex);
        }
        threeBlocks.Add(buffer);
        
        if(threeEnd!=BLOCK_SIZE) // Needs to be at least 2 blocks
        {
            var bytes = threeBlocks[0];
            for(int i=threeEnd; i<bytes.Length; i++)
                bytes[i] = 255;
            threeEnd = 0;
            threeBlocks.Add(Array.Empty<byte>());
            return;
        }

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

        if(threeStart+18>BLOCK_SIZE) // Key needs to be in the first block
        {
            byte[] block0 = threeBlocks[0], block1 = threeBlocks[1];
            Buffer.BlockCopy(block0, threeStart, block0, threeStart-18, BLOCK_SIZE-threeStart);
            Buffer.BlockCopy(block1, 0, block0, BLOCK_SIZE-18, 18);
            for(int i=0; i<18; i++) block1[i] = 255;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void check(Dictionary<long, Wrapper> dict, ref long rollingKey, byte nb, long mask)
    {
        if(nb==255) return;
        rollingKey = ((rollingKey & mask) << 2) | nb;
        Wrapper w;
        if (dict.TryGetValue(rollingKey, out w))
            w.v++;
        else
            dict[rollingKey] = new Wrapper();
    }

    static Task<string> count(int l, long mask, Func<Dictionary<long,Wrapper>,string> summary)
    {
        return Task.Run(() =>
        {
            long rollingKey = 0;
            var firstBlock = threeBlocks[0];
            var start = threeStart;
            while(--l>0) rollingKey = (rollingKey<<2) | firstBlock[start++];
            var dict = new Dictionary<long,Wrapper>();
            for(int i=start; i<firstBlock.Length; i++)
                check(dict, ref rollingKey, firstBlock[i], mask);

            int lastBlockId = threeBlocks.Count-1; 
            for(int bl=1; bl<lastBlockId; bl++)
            {
                var bytes = threeBlocks[bl];
                for(int i=0; i<bytes.Length; i++)
                    check(dict, ref rollingKey, bytes[i], mask);
            }

            var lastBlock = threeBlocks[lastBlockId];
            for(int i=0; i<threeEnd; i++)
                check(dict, ref rollingKey, lastBlock[i], mask);
            return summary(dict);
        });
    }

    static string writeFrequencies(Dictionary<long,Wrapper> freq, int fragmentLength)
    {
        var sb = new StringBuilder();
        double percent = 100.0 / freq.Values.Sum(i => i.v);
        foreach(var kv in freq.OrderByDescending(i => i.Value.v))
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
            sb.AppendLine((kv.Value.v * percent).ToString("F3"));
        }
        return sb.ToString();
    }

    static string writeCount(Dictionary<long,Wrapper> dictionary, string fragment)
    {
        long key = 0;
        for (int i=0; i<fragment.Length; ++i)
            key = (key << 2) | tonum[fragment[i]];
        Wrapper w;
        var n = dictionary.TryGetValue(key, out w) ? w.v : 0;
        return string.Concat(n.ToString(), "\t", fragment);
    }

    public static void MainOld(string[] args)
    {
        tonum['c'] = 1; tonum['C'] = 1;
        tonum['g'] = 2; tonum['G'] = 2;
        tonum['t'] = 3; tonum['T'] = 3;
        tonum['\n'] = 255; tonum['>'] = 255; tonum[255] = 255;

        LoadThreeData();

        Parallel.ForEach(threeBlocks, bytes =>
        {
            for(int i=0; i<bytes.Length; i++)
                bytes[i] = tonum[bytes[i]];
        });

        var task12 = count(12, 8388607, d => writeCount(d, "GGTATTTTAATT"));
        var task18 = count(18, 34359738367, d => writeCount(d, "GGTATTTTAATTTATAGT"));
        var task6 = count(6, 0b1111111111, d => writeCount(d, "GGTATT"));
        var task1 = count(1, 0, d => writeFrequencies(d, 1));
        var task2 = count(2, 0b11, d => writeFrequencies(d, 2));
        var task3 = count(3, 0b1111, d => writeCount(d, "GGT"));
        var task4 = count(4, 0b111111, d => writeCount(d, "GGTA"));
        
        task1.Wait();
        task2.Wait();
        task3.Wait();
        task4.Wait();
        task6.Wait();
        task12.Wait();
        task18.Wait();
        // Console.Out.WriteLineAsync(task1.Result);
        // Console.Out.WriteLineAsync(task2.Result);
        // Console.Out.WriteLineAsync(task3.Result);
        // Console.Out.WriteLineAsync(task4.Result);
        // Console.Out.WriteLineAsync(task6.Result);
        // Console.Out.WriteLineAsync(task12.Result);
        // Console.WriteLine(task18.Result);
    }
}