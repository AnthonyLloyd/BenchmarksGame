/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 * 
 * Regex-Redux
 * by Josh Goldfoot
 *
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public static class regexreduxImproved
{
    static int initialLength, strippedLength;
    static string[] sequences;

    static void strip()
    {
        var sequencesString = System.IO.File.ReadAllText(@"C:\temp\input5000000.txt");//Console.In.ReadToEnd();
        initialLength = sequencesString.Length;
        var starts = new List<int>();
        int removed = 0;
        sequencesString = Regex.Replace(sequencesString, ">.*\n|\n", (Match m) =>
        {
            if(m.Value[0]=='>') starts.Add(m.Index-removed);
            removed += m.Length;
            return String.Empty;
        });
        starts.Add(strippedLength = sequencesString.Length);
        sequences = new string[starts.Count-1];
        var noSequences = sequences.Length;

        for(int i=0; i<noSequences; i++)
            sequences[i] = sequencesString.Substring(starts[i],starts[i+1]-starts[i]);
        Console.Out.WriteLineAsync("Strip finish:"+DateTime.Now.ToString("HH:mm:ss.ffff"));
    }
    static Regex regex(string re)
    {
        var r = new Regex(re, RegexOptions.Compiled);
        //r.Matches("dummy");
        return r;
    }  
    public static void Main(string[] args)
    {
        Console.Out.WriteLineAsync("Start:"+DateTime.Now.ToString("HH:mm:ss.ffff"));
        var stripThread = new Thread(strip);
        stripThread.Start();

        var variants = new [] {
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

        var magic1 = regex("tHa[Nt]");
        var magic2 = regex("aND|caN|Ha[DS]|WaS");
        var magic3 = regex("a[NSt]|BY");
        var magic4 = regex("<[^>]*>");
        var magic5 = regex("\\|[^|][^|]*\\|");
        
        Console.Out.WriteLineAsync("Regex finish:"+DateTime.Now.ToString("HH:mm:ss.ffff"));
        
        stripThread.Join();
        var noSequences = sequences.Length;
        var counts = new int[10*noSequences];
        Parallel.For(0, 10*noSequences, i =>
        {
            var task = i / noSequences;
            var sequence = sequences[i % noSequences];
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
            var countStart = (i+1)*noSequences;
            var vCount = 0;
            for(int s=0; s<noSequences; s++) vCount += counts[countStart + s];
            Console.Out.WriteLineAsync(variants[i] + " " + vCount);
        }
        var sCount = 0;
        for(int s=0; s<noSequences; s++) sCount += counts[s];
        Console.Out.WriteLineAsync("\n"+initialLength+"\n"+strippedLength+"\n"+sCount);
    }
}