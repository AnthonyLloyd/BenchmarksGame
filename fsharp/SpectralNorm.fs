// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Based on C# version by Isaac Gouy, The Anh Tran, Alan McGovern
// Contributed by Don Syme

module SpectralNorm

#nowarn "9"

open System
open System.Threading
open Microsoft.FSharp.NativeInterop

type BarrierHandle(threads:int) =
    let mutable current = threads
    let mutable handle = new ManualResetEvent false
    member __.WaitOne() =
        let h = handle
        if Interlocked.Decrement(&current) > 0 then
            h.WaitOne() |> ignore
        else
            handle <- new ManualResetEvent false
            Interlocked.Exchange(&current, threads) |> ignore
            h.Set() |> ignore
            h.Dispose()

//[<EntryPoint>]
let main (args:string[]) =
    let n = if args.Length=0 then 2500 else int args.[0]
    let u = NativePtr.stackalloc n
    for i = 0 to n-1 do NativePtr.set u i 1.0
    let tmp = NativePtr.stackalloc n
    let v = NativePtr.stackalloc n

    let nthread = Environment.ProcessorCount
    let barrier = BarrierHandle nthread
    let chunk = n / nthread

    let inline approximate rbegin rend =
        // return element i,j of infinite matrix A
        let inline infA i j = 1.0 / float((i+j) * (i+j+1)/2 + i + 1)
        let inline infAt i j = infA j i

        // multiply vector v by matrix A
        let inline multiplyAv v res infM =
            for i = rbegin to rend do
                let mutable sum = 0.0
                for j = 0 to n-1 do
                    sum <- sum + infM i j * NativePtr.get<float> v j
                NativePtr.set res i sum

        // multiply vector v by matrix A and then by matrix A transposed
        let inline multiplyatAv v tmp atAv =
            multiplyAv v tmp infA
            barrier.WaitOne()
            multiplyAv tmp atAv infAt
            barrier.WaitOne()

        for __ = 0 to 9 do
            multiplyatAv u tmp v
            multiplyatAv v tmp u

        let mutable vBv = 0.0
        let mutable vv = 0.0

        for i = rbegin to rend do
            let ui = NativePtr.get u i
            let vi = NativePtr.get v i
            vBv <- vBv + ui * vi
            vv <- vv + vi * vi

        vBv, vv

    let aps =
        Array.init nthread (fun i ->
            let r1 = i * chunk
            let r2 = if i<nthread-1 then r1+chunk-1 else n-1
            async { return approximate r1 r2 } )
        |> Async.Parallel
        |> Async.RunSynchronously

    sqrt(Array.sumBy fst aps/Array.sumBy snd aps)
    // (sqrt (Array.sumBy fst aps/Array.sumBy snd aps)).ToString("F9")
    // |> stdout.WriteLine

    //0