/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 * 
 * Regex-Redux
 * by Josh Goldfoot
 *
*/

using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public static class regexreduxImproved
{
    public static void Main(string[] args)
    {
        var sequence = System.IO.File.ReadAllText(@"C:\temp\input5000000.txt");//Console.In.ReadToEnd();
        int initialLength = sequence.Length;

        sequence = Regex.Replace(sequence, ">.*\n|\n", String.Empty);

        var variants = new string[] {
            "agggtaaa|tttaccct"
            ,"[cgt]gggtaaa|tttaccc[acg]"
            ,"a[act]ggtaaa|tttacc[agt]t"
            ,"ag[act]gtaaa|tttac[agt]ct"
            ,"agg[act]taaa|ttta[agt]cct"
            ,"aggg[acg]aaa|ttt[cgt]ccct"
            ,"agggt[cgt]aa|tt[acg]accct"
            ,"agggta[cgt]a|t[acg]taccct"
            ,"agggtaa[cgt]|[acg]ttaccct"
        };
        
        var counts = new int[10];
        Parallel.For(0, 10, i =>
        {
            if(i==0)
            {
                var newseq = Regex.Replace(sequence, "tHa[Nt]", "<4>");
                newseq = Regex.Replace(newseq, "aND|caN|Ha[DS]|WaS", "<3>");
                newseq = Regex.Replace(newseq, "a[NSt]|BY", "<2>");
                newseq = Regex.Replace(newseq, "<[^>]*>", "|");
                newseq = Regex.Replace(newseq, "\\|[^|][^|]*\\|", "-");
                counts[0] = newseq.Length;
            }
            else
            {
                int c = 0;
                var m = Regex.Match(sequence, variants[i-1]);
                while(m.Success) { c++; m = m.NextMatch(); }
                counts[i] = c;
            }
        });

        for (int i=0; i<9; ++i) Console.Out.WriteLineAsync(variants[i] + " " + counts[i+1]);
        Console.Out.WriteLineAsync("\n"+initialLength+"\n"+sequence.Length+"\n"+counts[0]);
    }
}