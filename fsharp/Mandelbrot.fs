// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Adapted by Antti Lankila from the earlier Isaac Gouy's implementation
// Add multithread & tweaks from C++ by The Anh Tran
// Translate to F# by Jomo Fisher
// ported from C# version by Anthony Lloyd

#nowarn "9"

open System
open System.Numerics
open System.Runtime.CompilerServices
open System.Threading.Tasks
open Microsoft.FSharp.NativeInterop

let inline padd (p:nativeint) (i:int) = IntPtr.Add(p, 8*i)

let inline ptrGet (p:nativeint) i : Vector<float> =
    Unsafe.Read((padd p i).ToPointer())
let inline ptrSet (p:nativeint) i (v:Vector<float>) =
    Unsafe.Write((padd p i).ToPointer(), v)

let inline getByte (pcrbi:nativeint) (ciby:float) =
    let rec calc i res =
        if i=8 then res
        else
            let vCrbx = ptrGet pcrbi i
            let vCiby = Vector ciby
            let rec calc2 j zr zi b =
                let nZr = zr * zr - zi * zi + vCrbx
                let nZi = let zrzi = zr * zi in zrzi + zrzi + vCiby
                let t = nZr * nZr + nZi * nZi
                let b = b ||| (if t.[0]>4.0 then 2 else 0) ||| if t.[1]>4.0 then 1 else 0
                if b=3 || j=0 then b
                else calc2 (j-1) nZr nZi b
            calc (i+2) ((res<<<2) + calc2 48 vCrbx vCiby 0)
    calc 0 0 ^^^ -1 |> byte

[<EntryPoint>]
let main args =
    let size = if args.Length=0 then 200 else int args.[0]
    let lineLength = size >>> 3
    let s = "P4\n"+string size+" "+string size+"\n"
    let data = Array.zeroCreate (size*lineLength+s.Length)
    Text.ASCIIEncoding.ASCII.GetBytes(s, 0, s.Length, data, 0) |> ignore
    let crb = Array.zeroCreate (size+2)
    use pcrb = fixed &crb.[0]
    let pcrbi = NativePtr.toNativeInt pcrb
    let invN = Vector (2.0/float size)
    let onePtFive = Vector 1.5
    let step = Vector 2.0
    let rec loop i value =
        if i<size then
            ptrSet pcrbi i (value*invN-onePtFive)
            loop (i+2) (value+step)
    Vector [|0.0;1.0;0.0;0.0;0.0;0.0;0.0;0.0|] |> loop 0
    Parallel.For(0, size, fun y ->
        let ciby = crb.[y]+0.5
        for x = 0 to lineLength-1 do
            data.[y*lineLength+x] <- getByte (padd pcrbi (x*8)) ciby
    ) |> ignore
    //Console.OpenStandardOutput().Write(data, 0, data.Length)
    0