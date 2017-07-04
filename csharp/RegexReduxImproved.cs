/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 
   Regex-Redux by Josh Goldfoot
   parallelize by each sequence by Anthony Lloyd
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public static class regexreduxImproved
{
    static Regex[] variants;
    static Regex magic1, magic2, magic3, magic4, magic5;
    static Regex regex(string re)
    {
        var r = new Regex(re, RegexOptions.Compiled);
        // Regex doesn't look to be compiled on .Net Core, hence poor benchmark results.
        //r.Matches("dummy");
        return r;
    }
    static void createRegex()
    {
        variants = new [] {
             regex("agggtaaa|tttaccct")
            ,regex("[cgt]gggtaaa|tttaccc[acg]")
            ,regex("a[act]ggtaaa|tttacc[agt]t")
            ,regex("ag[act]gtaaa|tttac[agt]ct")
            ,regex("agg[act]taaa|ttta[agt]cct")
            ,regex("aggg[acg]aaa|ttt[cgt]ccct")
            ,regex("agggt[cgt]aa|tt[acg]accct")
            ,regex("agggta[cgt]a|t[acg]taccct")
            ,regex("agggtaa[cgt]|[acg]ttaccct")
        };

        magic1 = regex("tHa[Nt]");
        magic2 = regex("aND|caN|Ha[DS]|WaS");
        magic3 = regex("a[NSt]|BY");
        magic4 = regex("<[^>]*>");
        magic5 = regex("\\|[^|][^|]*\\|");
    }
    public static void Main(string[] args)
    {
        new Thread(createRegex).Start();

        var sequencesString = System.IO.File.ReadAllText(@"C:\temp\input5000000.txt");//Console.In.ReadToEnd();
        var initialLength = sequencesString.Length;
        var starts = new List<int>();
        int removed = 0;
        sequencesString = Regex.Replace(sequencesString, ">.*\n|\n", (Match m) =>
        {
            if(m.Value[0]=='>') starts.Add(m.Index-removed);
            removed += m.Length;
            return String.Empty;
        });

        starts.Add(sequencesString.Length);
        var sequences = new string[starts.Count-1];
        var noSequences = sequences.Length;

        for(int i=0; i<noSequences; i++)
            sequences[i] = sequencesString.Substring(starts[i],starts[i+1]-starts[i]);
        Array.Sort(sequences, (x,y) => -x.Length.CompareTo(y.Length));

        var counts = new int[10*noSequences];
        Parallel.For(0, 10*noSequences, i =>
        {
            var task = i%10;
            var sequence = sequences[i/10];
            if(task==0)
            {
                var newseq = magic1.Replace(sequence, "<4>");
                newseq = magic2.Replace(newseq, "<3>");
                newseq = magic3.Replace(newseq, "<2>");
                newseq = magic4.Replace(newseq, "|");
                newseq = magic5.Replace(newseq, "-");
                counts[i] = newseq.Length;
            }
            else
            {
                int c = 0;
                var m = variants[task-1].Match(sequence);
                while(m.Success) { c++; m = m.NextMatch(); }
                counts[i] = c;
            }
        });

        for (int i=0; i<9; ++i)
        {
            var vCount = 0;
            for(int s=i+1; s<counts.Length; s+=10) vCount += counts[s];
            Console.Out.WriteLineAsync(variants[i] + " " + vCount);
        }
        var sCount = 0;
        for(int s=0; s<counts.Length; s+=10) sCount += counts[s];
        Console.Out.WriteLineAsync("\n"+initialLength+"\n"+sequencesString.Length+"\n"+sCount);
    }
}