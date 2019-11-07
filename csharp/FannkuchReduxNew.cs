/* The Computer Language Benchmarks Game
   https://salsa.debian.org/benchmarksgame-team/benchmarksgame/

   contributed by Flim Nik
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
    static unsafe int CountFlips2(int first, short* state)
    {
        for (int flips = 2; ; flips++)
        {
            for (short* lo = state + 1, hi = state + first - 1; lo < hi; lo++, hi--)
            {
                var temp = *lo;
                *lo = *hi;
                *hi = temp;
            }
            var tp = state[first];
            if (state[tp] == 0) return flips;
            state[first] = (short)first;
            first = tp;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe int CountFlips(short* start, short* state, int length)
    {
        int first = start[0];
        if (start[first] == 0) return first == 0 ? 0 : 1;

        var startL = (long*)start;
        var stateL = (long*)state;
        var lengthL = length / 4;

        int i = 0;
        for (; i < lengthL; i++)
        {
            stateL[i] = startL[i];
        }
        for (i = lengthL * 4; i < length; i++)
        {
            state[i] = start[i];
        }

        return CountFlips2(first, state);
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

    public static string Main(string[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 7;
        fact = new int[n + 1];
        fact[0] = 1;

        for (int i = 1; i < fact.Length; i++)
        {
            fact[i] = fact[i - 1] * i;
        }

        var PC = 4;
        taskCount = n > 11 ? fact[n] / (9 * 8 * 7 * 6 * 5 * 4 * 3 * 2) : PC;
        int taskSize = fact[n] / taskCount;
        chkSums = new int[PC];
        maxFlips = new int[PC];
        var threads = new Thread[PC];
        for (int i = 1; i < PC; i++)
        {
            (threads[i] = new Thread(() => Run(n, taskSize))).Start();
        }
        Run(n, taskSize);

        for (int i = 1; i < threads.Length; i++)
        {
            threads[i].Join();
        }
        //Console.Out.WriteLineAsync(chkSums.Sum() + "\nPfannkuchen(" + n + ") = " + maxFlips.Max());
        return chkSums.Sum() + "\nPfannkuchen(" + n + ") = " + maxFlips.Max();
    }
}