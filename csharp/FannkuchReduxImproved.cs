/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, transliterated from Oleg Mazurov's Java program
   concurrency fix and minor improvements by Peperud
*/
namespace Improved
{

using System;
using System.Threading.Tasks;

public class FannkuchRedux
{
    const int INT_SIZE = 4;
    static int n;
    static int[] Fact;
    
    int[] p, pp, count;
    public int MaxFlips = 1;
    public int Chksum = 0;



    void FirstPermutation(int idx)
    {
        for (int i = 0; i < p.Length; ++i)
        {
            p[i] = i;
        }

        for (int i = count.Length - 1; i > 0; --i)
        {
            int d = idx / Fact[i];
            count[i] = d;
            idx = idx % Fact[i];

            Buffer.BlockCopy(p, 0, pp, 0, (i + 1) * INT_SIZE);

            for (int j = 0; j <= i; ++j)
            {
                p[j] = j + d <= i ? pp[j + d] : pp[j + d - i - 1];
            }
        }
    }

    void NextPermutation()
    {
        int first = p[1];
        p[1] = p[0];
        p[0] = first;

        int i = 1;
        while (++count[i] > i)
        {
            count[i++] = 0;
            int next = p[0] = p[1];
            for (int j = 1; j < i; ++j)
            {
                p[j] = p[j+1];
            }
            p[i] = first;
            first = next;
        }
    }

    int CountFlips()
    {
        int flips = 1;
        int first = p[0];
        if (p[first] != 0)
        {
            Buffer.BlockCopy(p, 0, pp, 0, pp.Length * INT_SIZE);
            do
            {
                ++flips;
                for (int lo = 1, hi = first - 1; lo < hi; ++lo, --hi)
                {
                    int t = pp[lo];
                    pp[lo] = pp[hi];
                    pp[hi] = t;
                }
                int tp = pp[first];
                pp[first] = first;
                first = tp;
            } while (pp[first] != 0);
        }
        return flips;
    }

    void Run(int i, int iMax)
    {   
        p = new int[n];
        pp = new int[n];
        count = new int[n];

        FirstPermutation(i);

        int maxflips = 1;
        int chksum = 0;
        for (;;)
        {
            if (p[0] != 0)
            {
                int flips = CountFlips();
                if (maxflips < flips) maxflips = flips;
                chksum += i % 2 == 0 ? flips : -flips;
            }

            if (++i == iMax) break;

            NextPermutation();
        }
        MaxFlips = maxflips;
        Chksum = chksum;
    }

    public static Tuple<int,int> Test(string[] args)
    {
        n = args.Length > 0 ? int.Parse(args[0]) : 7;
        
        Fact = new int[n];
        Fact[0] = 1;
        var fact = 1;
        for (int i=1; i<Fact.Length; i++) { Fact[i] = fact *= i; }
        fact *= n;

        var nTasks = Environment.ProcessorCount*4;
        var taskSize = (fact - 1) / nTasks + 1;
        var results = new FannkuchRedux[nTasks];
        Parallel.For(0, nTasks, t =>
        {
            var fr = new FannkuchRedux();
            results[t] = fr;
            var i = t*taskSize;
            fr.Run(i, Math.Min(fact, i + taskSize));
        });

        int res = 0, chk = 0;
        for (int i=0; i<results.Length; i++)
        {
            var result = results[i];
            chk += result.Chksum;
            if (res < result.MaxFlips) res = result.MaxFlips;
        }
        return Tuple.Create(chk,res);
    }
}

}