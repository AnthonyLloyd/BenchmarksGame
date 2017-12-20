// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Based on C# version by Isaac Gouy, The Anh Tran, Alan McGovern
// Contributed by Don Syme

module SpectralNormUp

#nowarn "9"

open System
open System.Threading
open Microsoft.FSharp.NativeInterop

let approximate n1 u tmp v rbegin rend (barrier: Barrier) =
    
    let inline multiplyAv v Av A =
        for i = rbegin to rend do
            let mutable sum = A i 0 * NativePtr.read v
            for j = 1 to n1 do
                sum <- sum + A i j * NativePtr.get<float> v j
            NativePtr.set Av i sum

    let inline multiplyatAv v tmp atAv =
        let inline matA i j = 1.0 / float((i + j) * (i + j + 1) / 2 + i + 1)
        multiplyAv v tmp matA
        barrier.SignalAndWait()
        let inline matAt i j = matA j i
        multiplyAv tmp atAv matAt
        barrier.SignalAndWait()

    for __ = 0 to 9 do
        multiplyatAv u tmp v
        multiplyatAv v tmp u

    let vbegin = NativePtr.get v rbegin
    let mutable vv = vbegin * vbegin
    let mutable vBv = vbegin * NativePtr.get u rbegin
    for i = rbegin+1 to rend do
        let vi = NativePtr.get v i
        vv <- vv + vi * vi
        vBv <- vBv + vi * NativePtr.get u i
    vBv, vv

//[<EntryPoint>]
let main (args:string[]) =
    let n = try int args.[0] with _ -> 2500
    let u = fixed &(Array.create n 1.0).[0]
    let tmp = NativePtr.stackalloc n
    let v = NativePtr.stackalloc n
    let nthread = Environment.ProcessorCount
    let barrier = new Barrier(nthread)
    let chunk = n / nthread
    let aps =
        [ for i = 0 to nthread-1 do
            let r1 = i * chunk
            let r2 = if (i < (nthread - 1)) then r1 + chunk - 1 else n-1
            yield async { return approximate (n-1) u tmp v r1 r2 barrier } ]
        |> Async.Parallel
        |> Async.RunSynchronously
    sqrt(Array.sumBy fst aps/Array.sumBy snd aps)

    //System.Console.WriteLine("{0:f9}", RunGame n);
    //0