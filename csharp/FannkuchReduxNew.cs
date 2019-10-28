/* The Computer Language Benchmarks Game
   https://salsa.debian.org/benchmarksgame-team/benchmarksgame/

   contributed by Isaac Gouy, transliterated from Oleg Mazurov's Java program
   concurrency fix and minor improvements by Peperud
   parallel and small optimisations by Anthony Lloyd
   "unsafe" array access by Jan de Vaan
*/

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

public unsafe static class FannkuchReduxNew
{
    static int taskCount;
    static int[] fact, chkSums, maxFlips;
    static void FirstPermutation(short* p, int[] count, int idx)
    {
        short[] pp = new short[count.Length];
        for (int i = 0; i < count.Length; ++i) p[i] = (byte)i;
        for (int i = count.Length - 1; i > 0; --i)
        {
            int d = idx / fact[i];
            count[i] = d;
            if (d > 0)
            {
                idx = idx % fact[i];
                for (int j = i; j >= 0; --j) pp[j] = p[j];
                for (int j = 0; j <= i; ++j) p[j] = pp[(j + d) % (i + 1)];
            }
        }
    }

    static void NextPermutation(short* p, int[] count)
    {
        var first = p[1];
        p[1] = p[0];
        p[0] = first;
        int i = 1;
        while (++count[i] > i)
        {
            count[i++] = 0;
            var next = p[1];
            p[0] = next;
            for (int j = 1; j < i;)
            {
                p[j] = p[++j];
            }
            p[i] = first;
            first = next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe int CountFlips(short* start, short* state, int length)
    {
        int first = start[0];
        if (start[first] == 0)
            return first == 0 ? 0 : 1;

        for (int i = 0; i < length; i++)
        {
            state[i] = start[i];
        }

        int flips = 2;
        for (; ; flips++)
        {
            short* lo = state + 1;
            short* hi = state + first - 1;

            for (; lo < hi; lo++, hi--)
            {
                var temp = *lo;
                *lo = *hi;
                *hi = temp;
            }
            var tp = state[first];
            if (state[tp] == 0)
                return flips;

            state[first] = (short)first;
            first = tp;
        }
    }

    static unsafe void Run(int n, int taskSize)
    {
        int[] count = new int[n];
        int taskId, chksum = 0, maxflips = 0;
        short* p = stackalloc short[n];
        short* state = stackalloc short[n];

        while ((taskId = Interlocked.Decrement(ref taskCount)) >= 0)
        {
            for (int i = 0; i < taskSize; i++)
            {
                if (i == 0)
                {
                    FirstPermutation(p, count, taskId * taskSize);
                }
                else
                {
                    NextPermutation(p, count);
                }
                var flips = CountFlips(p, state, n);
                chksum += (1 - (i & 1) * 2) * flips;
                if (flips > maxflips) maxflips = flips;
            }
        }
        chkSums[-taskId - 1] = chksum;
        maxFlips[-taskId - 1] = maxflips;
    }

    public static int Main(string[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 7;
        fact = new int[n + 1];
        fact[0] = 1;

        for (int i = 1; i < fact.Length; i++)
        {
            fact[i] = fact[i - 1] * i;
        }

        taskCount = n > 11 ? fact[n] / (9 * 8 * 7 * 6 * 5 * 4 * 3 * 2) : Environment.ProcessorCount;
        int taskSize = fact[n] / taskCount;
        int nThreads = Environment.ProcessorCount;
        chkSums = new int[nThreads];
        maxFlips = new int[nThreads];
        var threads = new Thread[nThreads];
        for (int i = 1; i < nThreads; i++)
        {
            (threads[i] = new Thread(() => Run(n, taskSize))).Start();
        }
        Run(n, taskSize);

        for (int i = 1; i < threads.Length; i++)
        {
            threads[i].Join();
        }
        //Console.Out.WriteLineAsync(chkSums.Sum() + "\nPfannkuchen(" + n + ") = " + maxFlips.Max());
        return chkSums.Sum() * 1000 + n * 100 + maxFlips.Max();
    }
}