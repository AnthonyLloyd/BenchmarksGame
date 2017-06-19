/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, transliterated from Oleg Mazurov's Java program
   concurrency fix and minor improvements by Peperud
*/
namespace Improved
{

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public class FannkuchRedux
{
    const int INT_SIZE = 4;
    static int n;
    static int taskSize, nTasks;
    static int MaxFlips, Chksum;
    static int[] Fact;
    
    int[] p, pp, count;

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // void rotate(int i,int d)
    // {
    //     Buffer.BlockCopy(p, 0, pp, 0, d * INT_SIZE);
    //     Buffer.BlockCopy(p, d * INT_SIZE, p, 0, (i-d) * INT_SIZE);
    //     Buffer.BlockCopy(pp, 0, p, (i-d) * INT_SIZE, d * INT_SIZE);
    // }
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
            if(d>0)
            {
                idx = idx % Fact[i];
                Buffer.BlockCopy(p, 0, pp, 0, (i + 1) * INT_SIZE);
                for (int j = 0; j <= i; ++j)
                {
                    p[j] = pp[(j+d)%(i+1)];
                }
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
            for (int j = 1; j<i; j++)
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
            for(;;)
            {
                flips++;
                for (int lo = 1, hi = first - 1; lo < hi; lo++, hi--)
                {
                    int t = pp[lo];
                    pp[lo] = pp[hi];
                    pp[hi] = t;
                }
                int tp = pp[first];
                if (pp[tp]==0) break;
                pp[first] = first;
                first = tp;
            }
        }
        return flips;
    }

    void Run(int taskId)
    {
        p = new int[n];
        pp = new int[n];
        count = new int[n];

        int maxflips = 1;
        int chksum = 0;

        do
        {
            var i = taskId*taskSize;
            var iMax = Math.Min(i+taskSize, Fact[n]);
            FirstPermutation(i);
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
            taskId = Interlocked.Decrement(ref nTasks);
        } while(taskId>=0);

        if (MaxFlips < maxflips) MaxFlips = maxflips;
        Chksum += chksum;
    }

    public static Tuple<int,int> Test(string[] args)
    {
        n = args.Length > 0 ? int.Parse(args[0]) : 7;
        
        Fact = new int[n+1];
        Fact[0] = 1;
        var fact = 1;
        for (int i=1; i<Fact.Length; i++) { Fact[i] = fact *= i; }

        MaxFlips = 1;
        Chksum = 0;

        nTasks = 12*11*10*9*8;
        taskSize = (fact-1) / nTasks + 1;

        var tasks = new Thread[Environment.ProcessorCount-1];
        for(int i=0; i<tasks.Length; i++)
        {
            var taskId = --nTasks;
            var thread = new Thread(() => new FannkuchRedux().Run(taskId));
            thread.Start();
            tasks[i] = thread;
        }
        new FannkuchRedux().Run(--nTasks);
        for(int i=0; i<tasks.Length; i++)
        {
            tasks[i].Join();
        }
        return Tuple.Create(Chksum, MaxFlips);
    }
}

}