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

class WrapperImproved { public int v; }
public static class KNucleotideImproved
{
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

    static StringBuilder writeFrequencies(Dictionary<long, WrapperImproved> freq, int buflen, int fragmentLength)
    {
        var sb = new StringBuilder();
        double percent = 100.0 / (buflen - fragmentLength + 1);
        foreach(var kv in freq.OrderByDescending(i => i.Value.v))
        {       
            sb.Append(printKey(kv.Key, fragmentLength));
            sb.Append(" ");
            sb.AppendLine(((kv.Value.v+1) * percent).ToString("F3"));
        }
        return sb;
    }

    static string writeCount(Dictionary<long, WrapperImproved> dictionary, string fragment)
    {
        long key = 0;
        for (int i=0; i<fragment.Length; ++i)
        {
            key <<= 2;
            key |= tonum[fragment[i]];
        }
        WrapperImproved w;
        var n = dictionary.TryGetValue(key, out w) ? w.v+1 : 1;
        return n+"\t"+fragment;
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

    static Dictionary<long,WrapperImproved> calcDictionary(byte[] bytes, int l, int mask, int start, int end)
    {
        long rollingKey=0;
        while(--l>0)
        {
            rollingKey = (rollingKey << 2) | bytes[start++];
        }
        var dict = new Dictionary<long,WrapperImproved>();
        while(start<end)
        {
            rollingKey = ((rollingKey<<2) & mask) | bytes[start++];
            WrapperImproved w;
            if (dict.TryGetValue(rollingKey, out w))
                w.v++;
            else
                dict.Add(rollingKey, new WrapperImproved());        
        }
        return dict;
    }

    static Task<Dictionary<long,WrapperImproved>>[] dictionaryTasks(byte[] bytes, int l, int mask, int n)
    {
        int step = (bytes.Length-l)/n+1;
        var tasks = new Task<Dictionary<long,WrapperImproved>>[n];
        for(int i=0; i<n-1; i++)
        {
            var start = i*step;
            var end = start+l-1+step;
            tasks[i] = Task.Run(() => calcDictionary(bytes, l, mask, start, end));
        }
        tasks[n-1] = Task.Run(() => calcDictionary(bytes, l, mask, (n-1)*step, bytes.Length));
        return tasks;
    }

    public static void Main(string[] args)
    {
        tonum['c'] = 1; tonum['C'] = 1;
        tonum['g'] = 2; tonum['G'] = 2;
        tonum['t'] = 3; tonum['T'] = 3;

        byte[] bytes;
        using (var stream = new StreamReader(File.OpenRead(@"C:\temp\input25000000.txt")/*Console.OpenStandardInput()*/))
        {
            string line = stream.ReadLine();
            while(line[0]!='>' || line[1]!='T' || line[2]!='H' || line[3]!='R' || line[4]!='E' || line[5]!='E')
                line = stream.ReadLine();

            var bytesList = new List<byte>(1048576);

            while((line=stream.ReadLine()) != null && line[0]!='>')
            {
                for(int i=0; i<line.Length; ++i)
                {
                    bytesList.Add(tonum[line[i]]);
                }
            }

            bytes = bytesList.ToArray();
        }

        int nParallel = 4;//Environment.ProcessorCount

        var taskDict18 = dictionaryTasks(bytes, 18, 16383, nParallel);
        var taskDict12 = dictionaryTasks(bytes, 12, 4095, nParallel);
        var taskDict6 = dictionaryTasks(bytes, 6, 1023, nParallel);
        var taskDict4 = dictionaryTasks(bytes, 4, 255, nParallel);
        var taskDict3 = dictionaryTasks(bytes, 3, 63, nParallel);
        var taskDict2 = dictionaryTasks(bytes, 2, 15, 1);
        var taskDict1 = dictionaryTasks(bytes, 1, 3, 1);
        
        var taskString18 = Task.Factory.ContinueWhenAll(taskDict18, t => writeCount(merge(t), "GGTATTTTAATTTATAGT"));
        var taskString12 = Task.Factory.ContinueWhenAll(taskDict12, t => writeCount(merge(t), "GGTATTTTAATT"));
        var taskString6 = Task.Factory.ContinueWhenAll(taskDict6, t => writeCount(merge(t), "GGTATT"));
        var taskString4 = Task.Factory.ContinueWhenAll(taskDict4, t => writeCount(merge(t), "GGTA"));
        var taskString3 = Task.Factory.ContinueWhenAll(taskDict3, t => writeCount(merge(t), "GGT"));
        var taskString1and2 = Task.Factory.ContinueWhenAll(new []{taskDict1[0], taskDict2[0]}, t =>
        {
            int buflen = 0;
            foreach(var w in t[0].Result.Values) buflen += w.v+1;
            var sb = writeFrequencies(t[0].Result, buflen, 1);
            sb.AppendLine();
            sb.Append(writeFrequencies(t[1].Result, buflen, 2));
            return sb;
        });

        Console.WriteLine(taskString1and2.Result);
        Console.WriteLine(taskString3.Result);
        Console.WriteLine(taskString4.Result);
        Console.WriteLine(taskString6.Result);
        Console.WriteLine(taskString12.Result);
        Console.WriteLine(taskString18.Result);
    }
}