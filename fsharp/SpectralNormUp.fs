// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Based on C# version by Isaac Gouy, The Anh Tran, Alan McGovern
// Contributed by Don Syme

module SpectralNormUp

open System
open System.Threading

let approximate u v tmp rbegin rend (barrier: Barrier) =

    let mutable vBv = 0.0
    let mutable vv = 0.0

    // return element i,j of infinite matrix A
    let inline A i j = 1.0 / float((i + j) * (i + j + 1) / 2 + i + 1)
    let inline At i j = A j i
    // multiply vector v by matrix A 
    let inline multiplyAv v Av A =
        for i = rbegin to rend - 1 do
            let mutable sum = 0.0
            for j = 0 to Array.length v - 1 do
                sum <- sum + A i j * Array.get<float> v j
            Array.set Av i sum

    // multiply vector v by matrix A and then by matrix A transposed
    let inline multiplyatAv v tmp atAv =
        multiplyAv v tmp A
        barrier.SignalAndWait()
        multiplyAv tmp atAv At
        barrier.SignalAndWait()

    for __ = 0 to 9 do
        multiplyatAv u tmp v
        multiplyatAv v tmp u

    for i = rbegin to rend - 1 do
        let vi = Array.get v i
        vv <- vv + vi * vi
        vBv <- vBv + vi * Array.get u i

    vBv, vv


let runGame n =
    // create unit vector
    let u = Array.create n 1.0
    let tmp = Array.zeroCreate n
    let v = Array.zeroCreate n

    let nthread = Environment.ProcessorCount

    let barrier = new Barrier(nthread)
        // create thread and hand out tasks
    let chunk = n / nthread
        // objects contain result of each thread
    let aps = 
        Async.Parallel 
          [ for i in 0 .. nthread - 1 do
                let r1 = i * chunk
                let r2 = if (i < (nthread - 1)) then r1 + chunk else n
                yield async { return approximate u v tmp r1 r2 barrier } ]
         |> Async.RunSynchronously

    let vBv = aps |> Array.sumBy fst
    let vv = aps |> Array.sumBy snd

    Math.Sqrt(vBv / vv)

//[<EntryPoint>]
let main (args:string[]) =
    let n = try int <| args.[0] with _ -> 2500

    runGame n
    //System.Console.WriteLine("{0:f9}", RunGame n);
    //0