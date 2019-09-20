// The Computer Language Benchmarks Game
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
let LinesPerBlock = 1024
[<Literal>]
let BlockSize = 61440 // Width * LinesPerBlock

open System
open System.Threading.Tasks

//[<EntryPoint>]
let main (args:string []) =
    let n = if args.Length=0 then 1000 else Int32.Parse(args.[0])
    let out = new IO.MemoryStream()//Console.OpenStandardOutput()
    let tasks = Array.zeroCreate ((3*n-1)/BlockSize+(5*n-1)/BlockSize+4)
    let bytePool = Buffers.ArrayPool.Shared
    
    let writeRandom n offset d seed (vs:byte[]) (ps:float[]) =
        // cumulative probability
        let mutable total = ps.[0]
        for i = 1 to ps.Length-1 do
            total <- total + ps.[i]
            ps.[i] <- total
            
        let intPool = Buffers.ArrayPool.Shared
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
        for i = offset+1 to offset+(n-1)/BlockSize do
            let rnds = rnds BlockSize
            tasks.[i] <- Task.Run(fun () -> bytes BlockSize 0 rnds)
        let remaining = (n-1)%BlockSize+1
        let rnds = rnds remaining
        tasks.[offset+(n-1)/BlockSize+1] <-
            Task.Run(fun () -> bytes remaining d rnds)
        seed

    tasks.[0] <- Task.Run(fun () ->
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
        "\n>TWO IUB ambiguity codes\n"B, 26
    )

    let seed =
        [|0.27;0.12;0.12;0.27;0.02;0.02;0.02;
            0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02|]
        |> writeRandom (3*n) 0 -1 42 "acgtBDHKMNRSVWY"B
         
    tasks.[(3*n-1)/BlockSize+2] <-
        Task.FromResult("\n>THREE Homo sapiens frequency\n"B, 31)

    [|0.3029549426680;0.1979883004921;0.1975473066391;0.3015094502008|]
    |> writeRandom (5*n) ((3*n-1)/BlockSize+2) 0 seed "acgt"B
    |> ignore

    async {
        for i = 0 to tasks.Length-1 do
            let t = tasks.[i]
            let! bs,l = Async.AwaitTask t
            out.Write(bs,0,l)
            if l>200 then bytePool.Return bs
        out.WriteByte '\n'B
        return out.ToArray()
    }
    |> Async.RunSynchronously