using System;
using System.Threading;
using System.Diagnostics;

public class Dbg
{
    long totalTime = 0;
    int totalCount = 0;
    public long Start()
    {
        return Stopwatch.GetTimestamp();
    }
    public void Stop(long startTime)
    {
        Interlocked.Add(ref totalTime, Stopwatch.GetTimestamp()-startTime);
        Interlocked.Increment(ref totalCount);
    }
    public void Summary()
    {
        Console.Out.WriteLineAsync((((double)totalTime)/totalCount).ToString("F9") + " = " + totalTime + "/" + totalCount);
    }
    public static Dbg Counter1 = new Dbg();
    public static Dbg Counter2 = new Dbg();
}