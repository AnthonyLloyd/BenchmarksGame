/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 *
 * submitted by Josh Goldfoot
 * 
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class KNucleotideImproved
{
    public static void Main(string[] args)
    {   
        PrepareLookups();
        var buffer = GetBytesForThirdSequence();   
        var fragmentLengths = new[] { 1, 2, 3, 4, 6, 12, 18 };
        var dicts =
            (from fragmentLength in fragmentLengths.AsParallel()
             select CountFrequency(buffer, fragmentLength)).ToArray();
        int buflen = dicts[0].Values.Sum(i => i.v);
        WriteFrequencies(dicts[0], buflen, 1);
        WriteFrequencies(dicts[1], buflen, 2);
        WriteCount(dicts[2], "GGT");
        WriteCount(dicts[3], "GGTA");
        WriteCount(dicts[4], "GGTATT");
        WriteCount(dicts[5], "GGTATTTTAATT");
        WriteCount(dicts[6], "GGTATTTTAATTTATAGT");
    }

    private static void WriteFrequencies(Dictionary<ulong, Wrapper2> freq, int buflen, int fragmentLength)
    {

        double percent = 100.0 / (buflen - fragmentLength + 1);
        foreach(var kv in freq.OrderByDescending(i => i.Value.v))
        {
            Console.Write(PrintKey(kv.Key, fragmentLength));
            Console.Write(" ");
            Console.WriteLine((kv.Value.v * percent).ToString("f3"));
        }
        Console.WriteLine();
    }

    private static void WriteCount(Dictionary<ulong, Wrapper2> dictionary, string fragment)
    {
        ulong key = 0;
        var keybytes = Encoding.ASCII.GetBytes(fragment.ToLower());
        for (int i = 0; i < keybytes.Length; i++)
        {
            key <<= 2;
            key |= tonum[keybytes[i]];
        }
        Wrapper2 w;
        Console.WriteLine("{0}\t{1}",
            dictionary.TryGetValue(key, out w) ? w.v : 0,
            fragment);
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

    private static byte[] GetBytesForThirdSequence()
    {
        var bytes = new List<byte>();
        using (var r = File.OpenText("fastabig.txt"))//new StreamReader(Console.OpenStandardInput()))
        {
            while (!r.ReadLine().StartsWith(">THREE"));
            var line = r.ReadLine();
            while (line != null && line[0] != '>')
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(line));
                line = r.ReadLine();
            }
        }
        return bytes.ToArray();
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