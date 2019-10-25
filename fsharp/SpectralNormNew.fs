// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// Based on C# version by Isaac Gouy, The Anh Tran, Alan McGovern
// Contributed by Don Syme

module SpectralNormNew

open System
open System.Threading
open System.Runtime.Intrinsics
open System.Runtime.Intrinsics.X86

type f2 = Vector128<float>

let approximate (u:f2[]) (v:f2[]) (tmp:f2[]) s e (barrier:Barrier) =

    let A i j =
        Vector128.Create(
            1.0 / float((i + j) * (i + j + 1) / 2 + i + 1),
            1.0 / float((i + j + 1) * (i + j + 2) / 2 + i + 1)
        )

    let At i j =
        Vector128.Create(
            1.0 / float((i + j) * (i + j + 1) / 2 + j + 1),
            1.0 / float((i + j + 1) * (i + j + 2) / 2 + j + 2)
        )

    let multiplyAv (v:f2[]) (Av:f2[]) =
        for i = s to e do
            let mutable sum1 = Ssse3.Multiply(A (i*2) 0, v.[0])
            let mutable sum2 = Ssse3.Multiply(A (i*2+1) 0, v.[0])
            for j = 1 to v.Length - 1 do
                sum1 <- Ssse3.Add(sum1, Ssse3.Multiply(A (i*2) (j*2), v.[j]))
                sum2 <- Ssse3.Add(sum2, Ssse3.Multiply(A (i*2+1) (j*2), v.[j]))
            Av.[i] <- Ssse3.HorizontalAdd(sum1, sum2)

    let multiplyAtv (v:f2[]) (atv:f2[]) =
        for i = s to e do
            let mutable sum1 = Ssse3.Multiply(At (i*2) 0, v.[0])
            let mutable sum2 = Ssse3.Multiply(At (i*2+1) 0, v.[0])
            for j = 1 to v.Length - 1 do
                sum1 <- Ssse3.Add(sum1, Ssse3.Multiply(At (i*2) (j*2), v.[j]))
                sum2 <- Ssse3.Add(sum2, Ssse3.Multiply(At (i*2+1) (j*2), v.[j]))
            atv.[i] <- Ssse3.HorizontalAdd(sum1, sum2)

    let multiplyatAv (v:f2[]) (tmp:f2[]) (atAv:f2[]) =
        multiplyAv v tmp
        barrier.SignalAndWait()
        multiplyAtv tmp atAv
        barrier.SignalAndWait()

    for _ = 0 to 9 do
        multiplyatAv u tmp v
        multiplyatAv v tmp u

    let mutable vBv = f2.Zero
    let mutable vv = f2.Zero

    for i = s to e do
        let v = v.[i]
        vBv <- Ssse3.Add(vBv, Ssse3.Multiply(u.[i], v))
        vv <- Ssse3.Add(vv, Ssse3.Multiply(v, v))

    vBv, vv

//[<EntryPoint>]
let main (args:string[]) =
    let n = try int args.[0] with _ -> 2500
    let n = (n + 1) / 2
    let u = Vector128.Create 1.0 |> Array.create n
    let tmp = Array.zeroCreate<f2> n
    let v = Array.zeroCreate<f2> n
    let NP = Environment.ProcessorCount
    let barrier = new Barrier(NP)
    let chunk = n / NP
    let aps = Async.Parallel [
                for i = 0 to NP - 2 do
                    let s = i * chunk
                    async { return approximate u v tmp s (s+chunk-1) barrier }
                async { return approximate u v tmp ((NP-1)*chunk) (n-1) barrier }
              ] |> Async.RunSynchronously

    let mutable vBv,vv = aps.[0]

    for i = 1 to aps.Length-1 do
        let t,b = aps.[i]
        vBv <- Ssse3.Add(vBv, t)
        vv <- Ssse3.Add(vv, b)
    vBv <- Ssse3.HorizontalAdd(vBv, vv)
    sqrt(vBv.GetElement 0 / vBv.GetElement 1).ToString("f9")
    
    //System.Console.WriteLine("{0:f9}", RunGame n);
    //0