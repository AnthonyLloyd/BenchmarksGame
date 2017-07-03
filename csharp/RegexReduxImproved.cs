/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 * 
 * Regex-Redux
 * by Josh Goldfoot
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public static class regexreduxImproved
{
    public static void Main(string[] args)
    {
        var sequence = System.IO.File.ReadAllText(@"C:\temp\input5000000.txt");//Console.In.ReadToEnd();
        int initialLength = sequence.Length;

        sequence = Regex.Replace(sequence, ">.*\n|\n", "");
        int codeLength = sequence.Length;

        var substitution = Task.Run(() => {
            string newseq = Regex.Replace(sequence, "tHa[Nt]", "<4>");
            newseq = Regex.Replace(newseq, "aND|caN|Ha[DS]|WaS", "<3>");
            newseq = Regex.Replace(newseq, "a[NSt]|BY", "<2>");
            newseq = Regex.Replace(newseq, "<[^>]*>", "|");
            newseq = Regex.Replace(newseq, "\\|[^|][^|]*\\|" , "-");
            return newseq.Length;
        });

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

        var tasks = new Task<int>[9];
        for(int i=0; i<9; i++)
        {
            var variant = variants[i];
            tasks[i] = Task.Run(() => Regex.Matches(sequence, variant).Count);
        }

        for (int i = 0; i < 9; i++)
            Console.WriteLine("{0} {1}", variants[i], tasks[i].Result);

        Console.WriteLine("\n{0}\n{1}\n{2}",
           initialLength, codeLength, substitution.Result);
    }
}