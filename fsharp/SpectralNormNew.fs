// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// Based on C# version by Isaac Gouy, The Anh Tran, Alan McGovern
// Contributed by Don Syme

module SpectralNormNew

open System
open System.Threading

let approximate (u:float[]) (v:float[]) (tmp:float[]) s e (barrier:Barrier) =

    let inline A i j = 1.0 / float((i + j) * (i + j + 1) / 2 + i + 1)

    let inline multiplyAv (v:float[]) (Av:float[]) =
        for i = s to e do
            let mutable sum = A i 0 * v.[0]
            for j = 1 to v.Length - 1 do
                sum <- sum + A i j * v.[j]
            Av.[i] <- sum

    let inline multiplyAtv (v:float[]) (atv:float[]) =
        for i = s to e do
            let mutable sum = A 0 i * v.[0]
            for j = 1 to v.Length - 1 do
                sum <- sum + A j i * v.[j]
            atv.[i] <- sum

    let multiplyatAv (v:float[]) (tmp:float[]) (atAv:float[]) =
        multiplyAv v tmp
        barrier.SignalAndWait()
        multiplyAtv tmp atAv
        barrier.SignalAndWait()

    for _ = 0 to 9 do
        multiplyatAv u tmp v
        multiplyatAv v tmp u

    let mutable vBv = 0.0
    let mutable vv = 0.0

    for i = s to e do
        let v = v.[i]
        vBv <- vBv + u.[i] * v
        vv <- vv + v * v

    vBv, vv

//[<EntryPoint>]
let main (args:string[]) =
    let n = try int args.[0] with _ -> 2500
    let u = Array.create n 1.0
    let tmp = Array.zeroCreate n
    let v = Array.zeroCreate n
    let NP = Environment.ProcessorCount
    let barrier = new Barrier(NP)
    let chunk = n / NP
    let aps = Async.Parallel [
                for i = 0 to NP - 2 do
                    let s = i * chunk
                    async { return approximate u v tmp s (s+chunk-1) barrier }
                async { return approximate u v tmp ((NP-1)*chunk) (n-1) barrier }
              ] |> Async.RunSynchronously
    sqrt(Array.sumBy fst aps / Array.sumBy snd aps)
    //System.Console.WriteLine("{0:f9}", RunGame n);
    //0