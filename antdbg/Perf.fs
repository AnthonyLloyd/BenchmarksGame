namespace global

open System.Diagnostics
open System.Threading

[<AutoOpen>]
module private Auto =
    let inline sqr x = x * x

type Estimate = Estimate of median:float * error:float
    with
    static member (-)(a:float,Estimate(m,e)) = Estimate(a-m, e)
    static member (-)(Estimate(m,e),a:float) = Estimate(m-a, e)
    static member (*)(Estimate(m,e),a:float) = Estimate(m*a, e*a)
    static member (/)(Estimate(m1,e1),Estimate(m2,e2)) =
        Estimate(m1/m2, sqrt(sqr(e1/m1)+sqr(e2/m2))*abs(m1/m2))
    override e.ToString() =
        let (Estimate(m,e)) = e
        m.ToString("0.0").PadLeft 5 + " ±" + e.ToString("0.0").PadLeft 4

module private Statistics =
    let estimate (s:ListSlim<int>) =
        let l = s.Count
        let m = if l % 2 = 0 then float(s.[l/2] + s.[l/2-1]) * 0.5
                else float s.[l/2]
        let e = sqrt(Seq.sumBy (fun i -> sqr(float i-m)) (s.ToSeq())
                / float(l*(l-1))) * 1.253
        Estimate(m,e)

[<Struct>]
type PerfRegion =
    PerfRegion of string * int64 * totalDelay: int64 * delayOnSince: int64

module Perf =
    type private PerfDelay =
        | Nothing
        | CollectTimes
        | Delay of string * int
    let mutable private perfRun = Nothing
    let mutable private times = ListSlim()
    let mutable private delayLock = SpinLock false
    let mutable private delayCount = 0
    let mutable private delayOnSince = 0L
    let mutable private totalDelay = 0L
    let regionStart (name:string) =
        match perfRun with
        | Nothing -> PerfRegion(null, 0L, 0L, 0L)
        | CollectTimes ->
            let now = Stopwatch.GetTimestamp()
            PerfRegion (name, now, totalDelay, delayOnSince)
        | Delay(n,d) ->
            let now = Stopwatch.GetTimestamp()
            if n=name && d<0 then
                let mutable lockTaken = false
                delayLock.Enter(&lockTaken)
                if delayCount = 0 then
                    if delayOnSince <> 0L then failwithf "um %i" delayOnSince
                    delayOnSince <- now
                delayCount <- delayCount + 1
                if lockTaken then delayLock.Exit true
            PerfRegion (name, now, totalDelay, delayOnSince)
    let regionEnd (PerfRegion (name,start,startTotalDelay,startDelayOnSince)) =
        match perfRun with
        | Nothing -> ()
        | CollectTimes ->
            let time = Stopwatch.GetTimestamp() - start
            lock times (fun () -> times.Add (struct (name,int time)) |> ignore)
        | Delay (_,0) -> Stopwatch.GetTimestamp() |> ignore
        | Delay (n,d) when n=name && d<0 ->
            let now = Stopwatch.GetTimestamp()
            let mutable lockTaken = false
            delayLock.Enter(&lockTaken)
            delayCount <- delayCount - 1
            if delayCount = 0 then
                if delayOnSince=0L then failwith "o no" //////////////////////////////
                totalDelay <- totalDelay + (now-delayOnSince) * int64 d / -100L
                delayOnSince <- 0L
            if lockTaken then delayLock.Exit true
        | Delay (n,d) when n=name ->
            let now = Stopwatch.GetTimestamp()
            let wait = now + (now-start) * int64 d / 100L
            if (wait-now) / Stopwatch.Frequency > 1L then printfn "%i %i\u001b[1F" (wait-now) now
            while Stopwatch.GetTimestamp() < wait do ()
        | Delay (_,d) when d>0 -> Stopwatch.GetTimestamp() |> ignore
        | Delay (_,d) ->
            let now = Stopwatch.GetTimestamp()
            let mutable lockTaken = false
            delayLock.Enter(&lockTaken)
            let totalDelay = totalDelay
            let delayOnSince = delayOnSince
            let delayCount = delayCount
            if (delayCount = 0) <> (delayOnSince = 0L) then failwithf "oh %i %i" delayCount delayOnSince
            if lockTaken then delayLock.Exit true
            let wait =
                now + totalDelay - startTotalDelay +
                ((if delayOnSince=0L then 0L else now-delayOnSince) +
                 (if startDelayOnSince=0L then 0L else startDelayOnSince-start)
                ) * int64 d / -100L
            if (wait-now) / Stopwatch.Frequency > 1L then printfn "%i %i %i %i %i %i %i %i %i\u001b[1F" ((wait-now)/Stopwatch.Frequency) d wait start now totalDelay startTotalDelay delayOnSince startDelayOnSince
            while Stopwatch.GetTimestamp() < wait do ()

    let causalProfiling n (f:unit->unit) =
        let clear() = times <- ListSlim times.Count
        let summary() =
            times.ToArray()
            |> Seq.groupBy (fun struct (n,_) -> n)
            |> Seq.map (fun (r,s) ->
            {|
                Region = r
                Count = Seq.length s
                Time = Seq.sumBy (fun struct (_,t) -> t) s |> float
            |})
            |> Seq.toList
        let run (d:PerfDelay) =
            perfRun <- d
            delayOnSince <- 0L
            delayCount <- 0
            totalDelay <- 0L
            let fStart = Stopwatch.GetTimestamp()
            f()
            int(Stopwatch.GetTimestamp() - fStart - totalDelay)
        printfn "Causal profiling"
        run CollectTimes |> ignore
        clear()
        let summaryTimePC = float(run CollectTimes) * 0.01
        let summary = summary()
        let names = summary |> Seq.map (fun i -> i.Region) |> Seq.toArray
        clear()

        let delays =
            Seq.collect (fun n ->
                Seq.map (fun i -> Delay(n,i)) [5;-5;10;-10;-15;-20]
            ) names
            |> Seq.append [Delay(null,0)]
            |> Seq.toArray

        let results = Seq.map (fun i -> i, ListSlim()) delays |> dict
        let median delay = Statistics.estimate results.[Delay delay]

        let printReport() =
            let totalTimePct = median(null,0) * 0.01
            "| Region         |  Count  |  Time%  |    +10%     |     +5%     \
             |     -5%     |    -10%     |    -15%     |    -20%     |\n\
             |:--------------:|:-------:|:-------:|:-----------:|:-----------:\
             |:-----------:|:-----------:|:-----------:|:-----------:|\n"
            + (Array.map (fun name ->
                  let s = summary |> List.find (fun i -> i.Region=name)
                  "| " + name.PadRight 14 +
                  " | " + s.Count.ToString().PadLeft 7 +
                  " | " + (s.Time/summaryTimePC).ToString("0.0").PadLeft 7 +
                  " | " + string(100.0 - median(name, 10)/totalTimePct) +
                  " | " + string(100.0 - median(name,  5)/totalTimePct) +
                  " | " + string(100.0 - median(name, -5)/totalTimePct) +
                  " | " + string(100.0 - median(name,-10)/totalTimePct) +
                  " | " + string(100.0 - median(name,-15)/totalTimePct) +
                  " | " + string(100.0 - median(name,-20)/totalTimePct) +
                  " |\n"
              ) names |> System.String.Concat)
            |> stdout.Write

        for i = 0 to n-1 do
            if i<>0 then printf "\u001b[%iF" (names.Length+3)
            printfn "Iterations: %i" i
            if i<>0 then printf "\u001b[%iE" (names.Length+2)
            Array.iter (fun d -> results.[d].AddSort(run d)) delays
            if i<>0 then printf "\u001b[%iF" (names.Length+2)
            printReport()

        perfRun <- Nothing