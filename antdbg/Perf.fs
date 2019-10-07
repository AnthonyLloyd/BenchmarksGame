namespace global

open System.Diagnostics

[<AutoOpen>]
module private Auto =
    let inline sqr x = x * x

type Estimate = Estimate of median:float * error:float
    with
    static member (-)(a:float,Estimate(m,e)) = Estimate(a-m, e)
    static member (*)(Estimate(m,e),a:float) = Estimate(m*a, e*a)
    static member (-)(Estimate(m1,e1),Estimate(m2,e2)) =
        Estimate(m1-m2, sqrt(sqr e1+sqr e2))
    static member (/)(Estimate(m1,e1),Estimate(m2,e2)) =
        Estimate(m1/m2, sqrt(sqr(e1/m1)+sqr(e2/m2))*abs(m1/m2))
    override e.ToString() =
        let (Estimate(m,e)) = e
        m.ToString("0.0").PadLeft 5 + " ±" + e.ToString("0.0").PadLeft 4

module private Statistics =
    let estimate s =
        let s = Seq.toArray s
        Array.sortInPlace s
        let l = Array.length s
        let m = if l % 2 = 0 then float(s.[l/2] + s.[l/2-1]) * 0.5
                else float s.[l/2]
        let e = sqrt(Array.sumBy (fun i -> sqr(float i-m)) s
                / float(l*(l-1))) * 1.253
        Estimate(m,e)

[<Struct>]
type PerfRegion = PerfRegion of string * int64

module Perf =
    type private PerfDelay =
        | Nothing
        | CollectTimes
        | Delay of string * int
    let mutable private times = ListSlim()
    let mutable private perfRun = Nothing
    let regionStart (name:string) =
        match perfRun with
        | Nothing -> PerfRegion(null, 0L)
        | _ -> PerfRegion (name, Stopwatch.GetTimestamp())
    let regionEnd (PerfRegion (name, start)) =
        let inline time() =
            let now = Stopwatch.GetTimestamp()
            let time = now - start
            now, time
        match perfRun with
        | Nothing -> ()
        | CollectTimes ->
            let _, time = time()
            lock times (fun () -> times.Add (struct (name,int time)) |> ignore)
        | Delay (n,d) ->
            let now, time = time()
            if d<>0 && ((d < 0) <> (n = name)) then
                let wait = now + time * int64(abs d) / 100L
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
            let fStart = Stopwatch.GetTimestamp()
            f()
            int(Stopwatch.GetTimestamp() - fStart)
        printfn "Causal profiling"
        run CollectTimes |> ignore
        clear()
        let summaryTimePC = float(run CollectTimes) * 0.01
        let summary = summary()
        let names = summary |> List.map (fun i -> i.Region)
        clear()

        let results = ListSlim()

        let printReport() =
            let results = results.ToArray()
            let median delay =
                results
                |> Seq.where (fun (d,_) -> d=Delay delay)
                |> Seq.map snd
                |> Statistics.estimate
            let totalTimePC = median(null,0) * 0.01
            "| Region         |  Count  |  Time%  |    +10%     |     +5%     \
             |     -5%     |    -10%     |    -15%     |    -20%     |\n\
             |:--------------:|:-------:|:-------:|:-----------:|:-----------:\
             |:-----------:|:-----------:|:-----------:|:-----------:|"
            |> stdout.WriteLine
            List.iter (fun name ->
              let s = summary |> List.find (fun i -> i.Region=name)
              "| " + name.PadRight 14 +
              " | " + s.Count.ToString().PadLeft 7 +
              " | " + (s.Time/summaryTimePC).ToString("0.0").PadLeft 7 +
              " | " + string(100.0-median(name,10)/totalTimePC) +
              " | " + string(100.0-median(name, 5)/totalTimePC) +
              " | " + string((median(null, -5)-median(name, -5))/totalTimePC) +
              " | " + string((median(null,-10)-median(name,-10))/totalTimePC) +
              " | " + string((median(null,-15)-median(name,-15))/totalTimePC) +
              " | " + string((median(null,-20)-median(name,-20))/totalTimePC) +
              " |" |> stdout.WriteLine
            ) names

        for i = 0 to n-1 do
            if i<>0 then printf "\u001b[%iF" (names.Length+3)
            printfn "Iterations: %i" i
            if i<>0 then printf "\u001b[%iE" (names.Length+2)
            List.map (fun i -> Delay(null,i)) [0;-5;-10;-15;-20]
            @ List.collect (fun n ->
                List.map (fun i -> Delay(n,i)) [5;-5;10;-10;-15;-20]
            ) names
            |> List.map (fun d -> d, run d)
            |> List.iter (results.Add >> ignore)
            if i<>0 then printf "\u001b[%iF" (names.Length+2)
            printReport()

        perfRun <- Nothing