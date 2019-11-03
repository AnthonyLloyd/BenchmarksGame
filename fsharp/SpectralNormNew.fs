// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// Based on C# version by Isaac Gouy, The Anh Tran, Alan McGovern
// Contributed by Don Syme

module SpectralNormNew

open System
open System.Threading

type Barrier(threads:int) =
    [<VolatileField>]
    let mutable current, iteration = threads, 0
    member _.SignalAndWait() =
        let i = iteration
        if Interlocked.Decrement(&current) = 0 then
            current <- threads
            iteration <- i + 1
        else while iteration = i do ()

let approximate u v tmp rbegin rend (barrier:Barrier) =

    let A i j = 2.0 / float((i + j) * (i + j + 1) + (i + 1) * 2)

    let multiplyAv (v:_[]) (Av:_[]) =
        for i = rbegin to rend do
            let mutable sum = 0.0
            for j = 0 to v.Length - 1 do
                sum <- sum + A i j * v.[j]
            Av.[i] <- sum

    let multiplyAtv (v:_[]) (atv:_[]) =
        for i = rbegin to rend do
            let mutable sum = 0.0
            for j = 0 to v.Length - 1 do
                sum <- sum + A j i * v.[j]
            atv.[i] <- sum

    let multiplyatAv v tmp atAv =
        multiplyAv v tmp
        barrier.SignalAndWait()
        multiplyAtv tmp atAv
        barrier.SignalAndWait()

    for _ = 0 to 9 do
        multiplyatAv u tmp v
        multiplyatAv v tmp u

    let mutable vBv, vv = 0.0, 0.0
    for i = rbegin to rend do
        vBv <- vBv + u.[i] * v.[i]
        vv <- vv + v.[i] * v.[i]
    vBv, vv

let runGame n =
    let u = Array.create n 1.0
    let tmp = Array.zeroCreate n
    let v = Array.zeroCreate n
    let barrier = new Barrier(4)
    let chunk = n / 4
    let aps =
        Array.Parallel.init 4 (fun i ->
            let r1 = i * chunk
            let r2 = if i=3 then n-1 else r1+chunk-1
            approximate u v tmp r1 r2 barrier
        )
    Math.Sqrt(Array.sumBy fst aps / Array.sumBy snd aps)

//[<EntryPoint>]
let main (args:string[]) =
    let n = try int args.[0] with _ -> 2500
    (runGame n).ToString("f9")//System.Console.WriteLine("{0:f9}", RunGame n);
    //0