﻿// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Contributed by Valentin Kraevskiy
// Multithreaded by Anthony Lloyd
module FastaNew

[<Literal>]
let Width = 60
[<Literal>]
let Width1 = 61
[<Literal>]
let LinesPerBlock = 2048

open System
open System.Threading.Tasks

//[<EntryPoint>]
let main (args:string []) =
    let n = if args.Length=0 then 1000 else Int32.Parse(args.[0])
    let out = new IO.MemoryStream()//Console.OpenStandardOutput()
    let tasks = 
        ((3*n-1)/(Width*LinesPerBlock)+(5*n-1)/(Width*LinesPerBlock)+3)
        |> Array.zeroCreate 
    let bytePool = Buffers.ArrayPool.Shared
    let intPool = Buffers.ArrayPool.Shared

    let writeRandom n offset d seed (vs:byte[]) (ps:float[]) =
        // cumulative probability
        let mutable total = ps.[0]
        for i = 1 to ps.Length-1 do
            total <- total + ps.[i]
            ps.[i] <- total
        
        let mutable seed = seed
        let inline rnds l =
            let a = intPool.Rent l
            for i = 0 to l-1 do
                seed <- (seed * 3877 + 29573) % 139968
                a.[i] <- seed
            a
        let bytes l d (rnds:int[]) =
            let a = bytePool.Rent (l+(l+d)/Width)
            let inline lookup probability =
                let rec search i =
                    if ps.[i]>=probability then i
                    else search (i+1)
                vs.[search 0]
            for i = 0 to l-1 do
                a.[i+i/Width] <- 1.0/139968.0 * float rnds.[i] |> lookup
            intPool.Return rnds
            for i = 1 to (l+d)/Width do
                a.[i*Width1-1] <- '\n'B
            a, l+(l+d)/Width

        for i = offset to offset+(n-1)/(Width*LinesPerBlock)-1 do
            let rnds = rnds (Width*LinesPerBlock)
            tasks.[i] <- Task.Run(fun () -> bytes (Width*LinesPerBlock) 0 rnds)

        //let nBlocks = offset+(n-1)/(Width*LinesPerBlock)-1
        //let rec createBlock i =
        //    let rnds = rnds (Width*LinesPerBlock)
        //    tasks.[i] <- Task.Run(fun () -> bytes (Width*LinesPerBlock) 0 rnds)
        //    if i < nBlocks then createBlock (i+1)
        //createBlock offset

        let remaining = (n-1)%(Width*LinesPerBlock)+1
        let rnds = rnds remaining
        tasks.[offset+(n-1)/(Width*LinesPerBlock)] <-
            Task.Run(fun () -> bytes remaining d rnds)
        seed

    Task.Run(fun () ->
        let seed =
            writeRandom (3*n) 0 -1 42 "acgtBDHKMNRSVWY"B
                [|0.27;0.12;0.12;0.27;0.02;0.02;0.02;
                  0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02|]
        
        tasks.[(3*n-1)/(Width*LinesPerBlock)+1] <-
            Task.FromResult("\n>THREE Homo sapiens frequency\n"B, 31)
        
        writeRandom (5*n) ((3*n-1)/(Width*LinesPerBlock)+2) 0 seed "acgt"B
            [|0.3029549426680;0.1979883004921;0.1975473066391;0.3015094502008|]
        |> ignore
    ) |> ignore

    ">ONE Homo sapiens alu\n"B |> fun i -> out.Write(i,0,i.Length)
    let table =
        "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG\
         GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA\
         CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT\
         ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA\
         GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG\
         AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC\
         AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA"B
    let tableLength = 287
    let linesPerBlock = (LinesPerBlock/tableLength+1) * tableLength
    let repeatedBytes = bytePool.Rent (Width1*linesPerBlock)
    for i = 0 to linesPerBlock*Width-1 do
        repeatedBytes.[i+i/Width] <- table.[i%tableLength]
    for i = 1 to linesPerBlock do
        repeatedBytes.[i*Width1-1] <- '\n'B
    for __ = 1 to (2*n-1)/(Width*linesPerBlock) do
        out.Write(repeatedBytes, 0, Width1*linesPerBlock)
    let remaining = (2*n-1)%(Width*linesPerBlock)+1
    if remaining<>0 then
        out.Write(repeatedBytes, 0, remaining+(remaining-1)/Width)
    bytePool.Return repeatedBytes
    out.Write("\n>TWO IUB ambiguity codes\n"B, 0, 26)

    let rec write i =
        if i<>tasks.Length then
            let t = tasks.[i]
            (if isNull t then i
             else
                let bs,l = t.Result
                out.Write(bs,0,l)
                if l>200 then bytePool.Return bs
                i+1) |> write
    write 0
    out.WriteByte '\n'B
    out.ToArray()