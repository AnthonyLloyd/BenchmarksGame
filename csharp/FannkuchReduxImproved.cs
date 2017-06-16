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
    static int n;
    static int[] Fact;
    
    int[] p, pp, count;

    const int INT_SIZE = 4;

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

    bool NextPermutation()
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
                p[j] = p[j + 1];
            }
            p[i] = first;
            first = next;
        }
        return true;
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

    Tuple<int,int> Run(int i, int iMax)
    {   
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

            if (++i == iMax)
            {
                break;
            }

            NextPermutation();
        }
        return Tuple.Create(maxflips, chksum);
    }

    public FannkuchRedux()
    {
        p = new int[n];
        pp = new int[n];
        count = new int[n];
    }

    // static void ParallelChunkFor(int n, int chunkSize, Action<int> a)
    // {
    //     var e = (n-1)/chunkSize + 1;
    //     Parallel.For(0, e, offset =>
    //     {
    //         offset *= chunkSize;
    //         var max = Math.Min(offset+chunkSize,n);
    //         for(int i=offset;i<max;i++)
    //             a(i);
    //     });
    // }

    public static Tuple<int,int> Test(string[] args)
    {
        n = args.Length > 0 ? int.Parse(args[0]) : 7;
        
        Fact = new int[n+1];
        Fact[0] = 1;
        var fact = 1;
        for (int i=1; i<Fact.Length; i++)
        {
            fact *= i;
            Fact[i] = fact;
        }

        var NCHUNKS = 150;
        var CHUNKSZ = (Fact[n] + NCHUNKS - 1) / NCHUNKS;
        var NTASKS = (Fact[n] + CHUNKSZ - 1) / CHUNKSZ;
        var maxFlips = new int[NTASKS];
        var chkSums = new int[NTASKS];

        Parallel.For(0, NTASKS, t =>
        {
            var i = t*CHUNKSZ;
            var tuple = new FannkuchRedux().Run(i, Math.Min(Fact[n], i + CHUNKSZ));
            maxFlips[t] = tuple.Item1;
            chkSums[t] = tuple.Item2;
        });

        int res = 0, chk = 0;
        for (int v=0; v < NTASKS; v++)
        {
            chk += chkSums[v];
        }
        for (int v=0; v < NTASKS; v++)
        {
            if (res < maxFlips[v]) res = maxFlips[v];
        }
        return Tuple.Create(chk,res);
    }
}

}