// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Based on C# version by Isaac Gouy, The Anh Tran, Alan McGovern
// Contributed by Don Syme

module SpectralNorm

#nowarn "9"

open Microsoft.FSharp.NativeInterop

//[<EntryPoint>]
let main (args:string[]) =
    let n = if Array.isEmpty args then 2500 else int args.[0]
    let u = NativePtr.stackalloc n
    for i = 0 to n-1 do NativePtr.set u i 1.0
    let tmp = NativePtr.stackalloc n
    let v = NativePtr.stackalloc n
    let nthread = System.Environment.ProcessorCount
    let barrier = new System.Threading.Barrier(nthread)

    let inline approximate rbegin rend =
        
        let inline multiplyAv v res infM =
            for i = rbegin to rend do
                let mutable sum = 0.0
                for j = 0 to n-1 do
                    sum <- sum + NativePtr.get<float> v j * infM i j
                NativePtr.set res i sum

        let inline multiplyatAv v tmp atAv =
            let inline infA i j = 1.0 / float((i+j) * (i+j+1)/2 + i + 1)
            multiplyAv v tmp infA
            barrier.SignalAndWait()
            let inline infAt i j = infA j i
            multiplyAv tmp atAv infAt
            barrier.SignalAndWait()

        for __ = 0 to 9 do
            multiplyatAv u tmp v
            multiplyatAv v tmp u

        let mutable vBv, vv = 0.0, 0.0
        for i = rbegin to rend do
            let vi = NativePtr.get v i
            vv <- vv + vi * vi
            vBv <- vBv + vi * NativePtr.get u i
        vBv, vv

    Array.init nthread (fun i -> async {
        let r1 = n/nthread * i
        let r2 = if i=nthread-1 then n-1 else r1+n/nthread-1
        return approximate r1 r2 } )
    |> Async.Parallel
    |> Async.RunSynchronously
    |> Array.reduce (fun (f1,s1) (f2,s2) -> f1+f2, s1+s2)
    ||> (/) |> sqrt

    // (sqrt (Array.sumBy fst aps/Array.sumBy snd aps)).ToString("F9")
    // |> stdout.WriteLine

    //0