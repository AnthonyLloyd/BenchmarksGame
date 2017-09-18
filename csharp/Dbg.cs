using System;
using System.Threading;
using System.Diagnostics;

public class Dbg
{
    long time = 0;
    public void Start()
    {
        time = Stopwatch.GetTimestamp();
    }
    public void Checkpoint(string name)
    {
        var diff = Stopwatch.GetTimestamp() - time;
        Console.Out.WriteLineAsync(name + " " + diff);
        Start();
    }
    public static Dbg Instance = new Dbg();
}