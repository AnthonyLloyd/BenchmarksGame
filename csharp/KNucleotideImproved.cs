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
using System.Threading.Tasks;

class WrapperImproved { public int v; }
public static class KNucleotideImproved
{
    const int COUNT_LENGTH = 7;
    readonly static int[] countKeyLengths = new int[COUNT_LENGTH] {18,12,6,4,3,2,1};

    static byte[] tonum = new byte[256];
    static char[] tochar = new char[] {'A', 'C', 'G', 'T'};

    static string printKey(long key, int fragmentLength)
    {
        var items = new char[fragmentLength];
        for (int i=fragmentLength-1; i>=0; --i)
        {
            items[i] = tochar[key & 0x3];
            key >>= 2;
        }
        return new string(items);
    }

    static void writeFrequencies(Dictionary<long, WrapperImproved> freq, int buflen, int fragmentLength)
    {
        var sb = new StringBuilder();
        double percent = 100.0 / (buflen - fragmentLength + 1);
        foreach(var kv in freq.OrderByDescending(i => i.Value.v))
        {       
            sb.Append(printKey(kv.Key, fragmentLength));
            sb.Append(" ");
            sb.AppendLine(((kv.Value.v+1) * percent).ToString("F3"));
        }
        Console.Out.WriteLine(sb.ToString());
    }

    static void writeCount(Dictionary<long, WrapperImproved> dictionary, string fragment)
    {
        long key = 0;
        for (int i=0; i<fragment.Length; ++i)
        {
            key <<= 2;
            key |= tonum[fragment[i]];
        }
        WrapperImproved w;
        var n = dictionary.TryGetValue(key, out w) ? w.v+1 : 1;
        Console.Out.WriteLine(n+"\t"+fragment);
    }

    static Dictionary<long,WrapperImproved> merge(Task<Dictionary<long,WrapperImproved>>[] dicts)
    {
        var d0 = dicts[0].Result;
        for(int i=1; i<dicts.Length; ++i)
        {
            foreach(var kv in dicts[i].Result)
            {
                WrapperImproved w;
                if (d0.TryGetValue(kv.Key, out w))
                    w.v += kv.Value.v;
                else
                    d0.Add(kv.Key, kv.Value);
            }
        }
        return d0;
    }

    public static void Main(string[] args)
    {
        tonum['c'] = 1; tonum['C'] = 1;
        tonum['g'] = 2; tonum['G'] = 2;
        tonum['t'] = 3; tonum['T'] = 3;

        using (var stream = new StreamReader(File.OpenRead(@"C:\temp\input25000000.txt")/*Console.OpenStandardInput()*/))
        {

            string line = stream.ReadLine();
            while(line[0]!='>' || line[1]!='T' || line[2]!='H' || line[3]!='R' || line[4]!='E' || line[5]!='E')
                line = stream.ReadLine();

            var bytes = new List<byte>(1048576);

            while((line=stream.ReadLine()) != null && line[0]!='>')
            {
                for(int i=0; i<line.Length; ++i)
                {
                    bytes.Add(tonum[line[i]]);
                }
            }
        }

        var dictionaryTasks = new Task<Dictionary<long,WrapperImproved>>[COUNT_LENGTH][];
        for(int i=0; i<COUNT_LENGTH; i++)
        {
            int c = countKeyLengths[i];
            int n = Environment.ProcessorCount;
            
        }   
        
        var task0 = Task.Factory.ContinueWhenAll(dictionaryTasks[0], merge);
        
        var taskz = task0.ContinueWith(dict => "");
            
        var dict6 = countDictionary[6];
        int buflen = 0;
        foreach(var w in dict6.Values) buflen += w.v+1;
        writeFrequencies(dict6, buflen, countKeyLengths[6]);
        writeFrequencies(countDictionary[5], buflen, countKeyLengths[5]);
        writeCount(countDictionary[4], "GGT");
        writeCount(countDictionary[3], "GGTA");
        writeCount(countDictionary[2], "GGTATT");
        writeCount(countDictionary[1], "GGTATTTTAATT");
        writeCount(countDictionary[0], "GGTATTTTAATTTATAGT");
    }

    // static void process()
    // {
    //     const byte A = (byte)'a';//, C = (byte)'c', G = (byte)'g', T = (byte)'t';
    //     while(true)
    //     {
    //         bool finished = threeEnd>=0;
    //         int countId=0,working=0,lastId=threeBlockLastId;
    //         for(; countId<COUNT_LENGTH; countId++)
    //         {
    //             working = countBlockWorking[countId];
    //             if(working!=-1 && working<=lastId
    //             && Interlocked.CompareExchange(ref countBlockWorking[countId], -1, working)==working) break;
    //         }
            
    //         if(countId==COUNT_LENGTH)
    //         {
    //             if(finished) break;
    //             Thread.Sleep(0);
    //             continue;
    //         }
            
    //         // work block working
    //         if(working==0)
    //         {
    //             var dictionary = countDictionary[countId] = new Dictionary<long,WrapperImproved>();
    //             int i = threeStart;

    //             long rollingKey = 0;
    //             long mask = 0;
    //             var block = threeBlocks[working];
    //             var cursorEnd = countKeyLengths[countId]-1;
    //             for (int cursor = 0; cursor<cursorEnd; cursor++)
    //             {
    //                 rollingKey <<= 2;
    //                 rollingKey |= tonum[block[i]];
    //                 mask = (mask << 2) + 3;
                    
    //                 if(++i==READER_BUFFER_SIZE)
    //                 {
    //                     while(threeBlocks[1]==null) Thread.Sleep(0);
    //                     block = threeBlocks[1];
    //                     working = 1;
    //                     i = 0;
    //                 }
    //             }
    //             mask = (mask << 2) + 3;
    //             countMask[countId] = mask;

    //             for(;i<block.Length;i++)
    //             {
    //                 var cursorByte = block[i];
    //                 if(cursorByte>=A)
    //                 {
    //                     rollingKey = ((rollingKey << 2) & mask) | tonum[cursorByte];
    //                     WrapperImproved w;
    //                     if (dictionary.TryGetValue(rollingKey, out w))
    //                         w.v++;
    //                     else
    //                         dictionary.Add(rollingKey, new WrapperImproved());
    //                 }
    //             }
    //             countRollingKey[countId] = rollingKey;
    //         }
    //         else
    //         {
    //             var dictionary = countDictionary[countId];
    //             var rollingKey = countRollingKey[countId];
    //             var mask = countMask[countId];
    //             foreach(var cursorByte in threeBlocks[working])
    //             {
    //                 if(cursorByte>=A)
    //                 {   
    //                     rollingKey = ((rollingKey << 2) & mask) | tonum[cursorByte];
    //                     WrapperImproved w;
    //                     if (dictionary.TryGetValue(rollingKey, out w))
    //                         w.v++;
    //                     else
    //                         dictionary.Add(rollingKey, new WrapperImproved());
    //                 }
    //             }
    //             countRollingKey[countId] = rollingKey;
    //         }

    //         // end block working

    //         countBlockWorking[countId] = working+1;
    //     }
    // }
}