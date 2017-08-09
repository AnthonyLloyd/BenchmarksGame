/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, transliterated from Oleg Mazurov's Java program
   concurrency fix and minor improvements by Peperud
   parallel and small optimisations by Anthony Lloyd
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public static class FannkuchReduxImproved
{
    const long one = ((long)1) << 32;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void firstPermutation(int n, int[] fact, int[] p, int[] pp, int[] count, int idx)
    {
        for (int i=0; i<n; ++i) p[i] = i;
        for (int i=n-1; i>0; --i)
        {
            int d = idx/fact[i];
            count[i] = d;
            if(d>0)
            {
                idx = idx%fact[i];
                for (int j=i ;j>=0; --j) pp[j] = p[j];
                for (int j = 0; j <= i; ++j) p[j] = pp[(j+d)%(i+1)];
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int nextPermutation(int[] p, int[] count)
    {
        int first = p[1];
        p[1] = p[0];
        p[0] = first;
        int i = 1;
        while (++count[i] > i)
        {
            count[i++] = 0;
            int next = p[1];
            p[0] = next;
            for(int j=1;j<i;) p[j] = p[++j];
            p[i] = first;
            first = next;
        }
        return first;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long reduce(long l1, long l2)
    {
        return l1>l2 ? l1 + (l2 & int.MaxValue) : l2 + (l1 & int.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long countFlips(int n, int[] p, int[] pp, int first, int sign, long maxflipsAndChksum)
    {
        if (first==0) return maxflipsAndChksum;
        if (p[first]==0) return maxflipsAndChksum + (long)sign;
        for(int i=0; i<n; i++) pp[i] = p[i];
        uint flips = 2;
        while(true)
        {
            for (int lo=1, hi=first-1; lo<hi; lo++,hi--)
            {
                int t = pp[lo];
                pp[lo] = pp[hi];
                pp[hi] = t;
            }
            int tp = pp[first];
            if (pp[tp]==0)
            {
                maxflipsAndChksum += (long)(sign * flips);
                if(flips>(maxflipsAndChksum >> 32)) maxflipsAndChksum = ((long)flips << 32) + (maxflipsAndChksum & int.MaxValue);
                return maxflipsAndChksum;
            }
            pp[first] = first;
            first = tp;
            flips++;
        }
    }

    static long run(int n, int[] fact, int taskId, int taskSize, long maxflipsAndChksum)
    {
        int[] p = new int[n], pp = new int[n], count = new int[n];
        firstPermutation(n, fact, p, pp, count, taskId*taskSize);
        maxflipsAndChksum = countFlips(n, p, pp, p[0], 1, maxflipsAndChksum);
        while (--taskSize>0)
        {
            maxflipsAndChksum = countFlips(n, p, pp, nextPermutation(p, count), 1-(taskSize%2)*2, maxflipsAndChksum);
        }
        return maxflipsAndChksum;
    }

    public static void Main(string[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 7;
        var fact = new int[n+1];
        fact[0] = 1;
        var factn = 1;
        for (int i=1; i<fact.Length; i++) { fact[i] = factn *= i; }

        int nTasks = 2*3*4*5;//Environment.ProcessorCount;
        int taskSize = factn/nTasks;
        long maxflipsAndChksum = 0;
        Parallel.For(0, nTasks
            , () => ((long)1) << 32
            , (i,_,l) => run(n, fact, i, taskSize, l)
            , l => {
                var chk = maxflipsAndChksum;
                while (Interlocked.CompareExchange(ref maxflipsAndChksum, reduce(chk, l), chk) != chk);
            }
        );
        Console.Out.WriteLineAsync((maxflipsAndChksum & uint.MaxValue) + "\nPfannkuchen("+n+") = " + (maxflipsAndChksum >> 32));
    }
}