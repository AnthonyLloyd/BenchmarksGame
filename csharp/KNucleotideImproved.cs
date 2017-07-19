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
    static byte[] tonum = new byte[256];
    static char[] tochar = new char[] {'A', 'C', 'G', 'T'};

    static string writeFrequencies(Dictionary<int, WrapperImproved> freq, int buflen, int fragmentLength)
    {
        var sb = new StringBuilder();
        double percent = 100.0 / (buflen - fragmentLength + 1);
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

    static string writeCount(Dictionary<int, WrapperImproved> dictionary, string fragment)
    {
        int key = 0;
        for (int i=0; i<fragment.Length; ++i)
        {
            key = (key << 2) | tonum[fragment[i]];
        }
        WrapperImproved w;
        var n = dictionary.TryGetValue(key, out w) ? w.v : 0;
        return n+"\t"+fragment;
    }

    static string writeCountLong(Dictionary<long, WrapperImproved> dictionary, string fragment)
    {
        long key = 0;
        for (int i=0; i<fragment.Length; ++i)
        {
            key = (key << 2) | tonum[fragment[i]];
        }
        WrapperImproved w;
        var n = dictionary.TryGetValue(key, out w) ? w.v : 0;
        return n+"\t"+fragment;
    }

    static Dictionary<int,WrapperImproved> merge(Task<Dictionary<int,WrapperImproved>>[] dicts)
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
                    d0[kv.Key] = kv.Value;
            }
        }
        return d0;
    }
    static Dictionary<long,WrapperImproved> mergeLong(Task<Dictionary<long,WrapperImproved>>[] dicts)
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
                    d0[kv.Key] = kv.Value;
            }
        }
        return d0;
    }

    static Dictionary<int,WrapperImproved> calcDictionary(byte[] bytes, int l, int mask, int start, int end)
    {
        int rollingKey = 0;
        while(--l>0)
        {
            rollingKey = (rollingKey << 2) | bytes[start++];
        }
        var dict = new Dictionary<int,WrapperImproved>();
        while(start<end)
        {
            rollingKey = ((rollingKey<<2) & mask) | bytes[start++];
            WrapperImproved w;
            if (dict.TryGetValue(rollingKey, out w))
                w.v++;
            else
                dict[rollingKey] = new WrapperImproved();
        }
        return dict;
    }

    static Dictionary<long,WrapperImproved> calcDictionaryLong(byte[] bytes, int l, long mask, int start, int end)
    {
        long rollingKey = 0;
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
                dict[rollingKey] = new WrapperImproved();
        }
        return dict;
    }

    static Task<Dictionary<int,WrapperImproved>> count(byte[] bytes, int l, int mask, int n)
    {
        if(n==1) return Task.Run(() => calcDictionary(bytes, l, mask, 0, bytes.Length));
        int step = (bytes.Length-l)/n+1;
        var tasks = new Task<Dictionary<int,WrapperImproved>>[n];
        for(int i=0; i<n; i++)
        {
            var start = i*step;
            var end = Math.Min(start+l-1+step, bytes.Length);
            tasks[i] = Task.Run(() => calcDictionary(bytes, l, mask, start, end));
        }
        return Task.Factory.ContinueWhenAll(tasks, merge);
    }

    static Task<Dictionary<long,WrapperImproved>> countLong(byte[] bytes, int l, long mask, int n)
    {
        int step = (bytes.Length-l)/n+1;
        var tasks = new Task<Dictionary<long,WrapperImproved>>[n];
        for(int i=0; i<n; i++)
        {
            var start = i*step;
            var end = Math.Min(start+l-1+step, bytes.Length);
            tasks[i] = Task.Run(() => calcDictionaryLong(bytes, l, mask, start, end));
        }
        return Task.Factory.ContinueWhenAll(tasks, mergeLong);
    }
    public static void Main(string[] args)
    {
        tonum['c'] = 1; tonum['C'] = 1;
        tonum['g'] = 2; tonum['G'] = 2;
        tonum['t'] = 3; tonum['T'] = 3;

        var lines = new List<string>(1024);
        var linePosition = new List<int>(1024);
        int position = 0;
        using (var stream = new StreamReader(File.OpenRead(@"C:\temp\input25000000.txt")/*Console.OpenStandardInput()*/))
        {
            string line = stream.ReadLine();
            while(line[0]!='>' || line[1]!='T' || line[2]!='H' || line[3]!='R' || line[4]!='E' || line[5]!='E')
                line = stream.ReadLine();

            while((line=stream.ReadLine()) != null && line[0]!='>')
            {
                lines.Add(line);
                linePosition.Add(position);
                position += line.Length;
            }            
        }

        int nParallel = Environment.ProcessorCount;

        var bytes = new byte[position];

        var tasks = new Task[nParallel];
        int step = (lines.Count-1)/nParallel+1;
        for(int t=0; t<tasks.Length; ++t)
        {
            int start = t*step;
            int end = Math.Min(start+step, lines.Count);
            tasks[t] = Task.Run(() =>
            {
                int index = linePosition[start];
                for(int i=start; i<end; ++i)
                    foreach(var c in lines[i])
                        bytes[index++] = tonum[c];
            });
        }

        Task.WaitAll(tasks);

        var taskString18 = countLong(bytes, 18, 68719476735/* 2**(2*18)-1 */, nParallel/2)
            .ContinueWith(t => writeCountLong(t.Result, "GGTATTTTAATTTATAGT"));
        
        var taskString12 = count(bytes, 12, 16777215/* 2**(2*12)-1 */, nParallel/2)
            .ContinueWith(t => writeCount(t.Result, "GGTATTTTAATT"));        
        
        var taskString6 = count(bytes, 6, 4095/* 2**(2*6)-1 */, nParallel/2)
            .ContinueWith(t => writeCount(t.Result, "GGTATT"));

        var taskString4 = count(bytes, 4, 255/* 2**(2*4)-1 */, nParallel/2)
            .ContinueWith(t => writeCount(t.Result, "GGTA"));
        
        var taskString3 = count(bytes, 3, 63/* 2**(2*3)-1 */, 1)
            .ContinueWith(t => writeCount(t.Result, "GGT"));
        
        var taskString2 = count(bytes, 2, 15/* 2**(2*2)-1 */, 1)
            .ContinueWith(t => writeFrequencies(t.Result, position, 2));
        
        var taskString1 = Task.Run(() =>
        {
            int a=0, c=0, g=0, t=0;
            foreach(var i in bytes)
            {
                switch(i)
                {
                    case 0: a++; break;
                    case 1: c++; break;
                    case 2: g++; break;
                    default: t++; break;
                }
            }
            var dict1 = new Dictionary<int,WrapperImproved>{
                {0, new WrapperImproved {v=a}},
                {1, new WrapperImproved {v=c}},
                {2, new WrapperImproved {v=g}},
                {3, new WrapperImproved {v=t}}
            };
            return writeFrequencies(dict1, position, 1);
        });
        
        Console.Out.WriteLineAsync(taskString1.Result);
        Console.Out.WriteLineAsync(taskString2.Result);
        Console.Out.WriteLineAsync(taskString3.Result);
        Console.Out.WriteLineAsync(taskString4.Result);
        Console.Out.WriteLineAsync(taskString6.Result);
        Console.Out.WriteLineAsync(taskString12.Result);
        Console.Out.WriteLineAsync(taskString18.Result);
    }
}