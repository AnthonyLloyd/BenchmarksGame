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

public static class FannkuchRedux
{
    const int INT_SIZE = 4;
    static int n, taskSize, nTasks;
    static int[] Fact;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void rotate(int[] p, int[] pp, int l, int d)
    {
        Buffer.BlockCopy(p, 0, pp, 0, d);
        Buffer.BlockCopy(p, d, p, 0, l);
        Buffer.BlockCopy(pp, 0, p, l, d);        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void firstPermutation(int[] p, int[] pp, int[] count, int idx)
    {
        for (int i=0; i<n; ++i) { p[i] = i; }
        for (int i=n-1; i>0; --i)
        {
            int d = idx/Fact[i];
            count[i] = d;
            if(d>0)
            {
                idx = idx%Fact[i];
                rotate(p, pp, (i+1-d) * INT_SIZE, d * INT_SIZE);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void nextPermutation(int[] p, int[] count)
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
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int countFlips(int[] p, int[] pp)
    {
        int flips = 1;
        int first = p[0];
        if (p[first] != 0)
        {
            for(int i=n-1;i>=0;--i) pp[i]=p[i];
            while(true)
            {
                flips++;
                for (int lo=1, hi=first-1; lo<hi; lo++,hi--)
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

    static Tuple<int,int> Run(int taskId)
    {
        int[] p = new int[n], pp = new int[n], count = new int[n];
        int maxflips=0, chksum=0;
        do
        {
            firstPermutation(p, pp, count, taskId*taskSize);
            if(p[0]!=0)
            {
                int firstFlips = countFlips(p, pp);
                chksum += firstFlips;
                if(firstFlips>maxflips) maxflips=firstFlips;
            }
            for (int i=1; i<taskSize; ++i)
            {
                nextPermutation(p, count);
                if (p[0] != 0)
                {
                    int flips = countFlips(p, pp);
                    chksum += i%2==0 ? flips : -flips;
                    if(flips>maxflips) maxflips=flips;
                }
            }
            taskId = Interlocked.Decrement(ref nTasks);
        } while(taskId>=0);
        return Tuple.Create(maxflips, chksum);
    }

    public static Tuple<int,int> Test(string[] args)
    {
        n = args.Length > 0 ? int.Parse(args[0]) : 7;
        
        Fact = new int[n+1];
        Fact[0] = 1;
        var fact = 1;
        for (int i=1; i<Fact.Length; i++) { Fact[i] = fact *= i; }

        nTasks = 2*4*5;
        taskSize = (fact-1) / nTasks + 1;

        var tasks = new Task<Tuple<int,int>>[4/*Environment.ProcessorCount*/];
        for(int i=0; i<tasks.Length; i++)
        {
            var taskId = --nTasks;
            tasks[i] = Task.Run(() => Run(taskId));
        }
        Task.WaitAll(tasks);

        int chksum=0, maxFlips=0;
        for(int i=0; i<tasks.Length; i++)
        {
            var result = tasks[i].Result;
            if(result.Item1>maxFlips) maxFlips=result.Item1;
            chksum += result.Item2;
        }
        return Tuple.Create(chksum, maxFlips);
    }
}

}