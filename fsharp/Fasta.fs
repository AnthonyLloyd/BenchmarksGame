// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Contributed by Valentin Kraevskiy
// multithreaded by Anthony Lloyd

[<Literal>]
let Width = 60
[<Literal>]
let Width1 = 61
[<Literal>]
let LinesPerBlock = 1024

open System
open System.Threading.Tasks

[<EntryPoint>]
let main args =
    let n = if args.Length=0 then 1000 else Int32.Parse(args.[0])
    let out = Console.OpenStandardOutput()
    let bytePool = System.Buffers.ArrayPool.Shared
    let intPool = System.Buffers.ArrayPool.Shared
    let im,ia,ic = 139968,3877,29573
    let oneOverIM = 1.0/139968.0
    let mutable seed = 42
    let inline rnds l =
        let a = intPool.Rent l
        for i = 0 to l-1 do
            seed <- (seed * ia + ic) % im
            a.[i] <- seed
        a
    let blockSize = Width*LinesPerBlock
    let tasks = Array.zeroCreate ((3*n-1)/blockSize+(5*n-1)/blockSize+3)

    Task.Run(fun () ->

        let writeRandom n o d (vs:byte[]) (ps:float[]) =
            let bytes l d (rnds:int[]) =
                let a = bytePool.Rent (l+(l+d)/Width)
                let inline lookup (r:int) =
                    let p = float r * oneOverIM
                    let rec search i =
                        if ps.[i]>=p then i
                        else search (i+1)
                    vs.[search 0]
                for i = 0 to l-1 do
                    a.[i+i/Width] <- lookup rnds.[i]
                intPool.Return rnds
                for i = 1 to (l+d)/Width do
                    a.[i*Width1-1] <- '\n'B
                a        
            for i = o to o+(n-1)/blockSize-1 do
                let rnds = rnds blockSize
                tasks.[i] <- Task.Run(fun () ->
                    bytes blockSize 0 rnds, blockSize+blockSize/Width)
            let remaining = (n-1)%blockSize+1
            let rnds = rnds remaining
            tasks.[o+(n-1)/blockSize] <- Task.Run(fun () ->
                bytes remaining d rnds, remaining+(remaining+d)/Width)

        let inline cumsum (ps:float[]) =
            let mutable total = ps.[0]
            for i = 1 to ps.Length-1 do
                total <- total + ps.[i]
                ps.[i] <- total
            ps

        cumsum [|0.27;0.12;0.12;0.27;0.02;0.02;0.02;
                 0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02|]
        |> writeRandom (3*n) 0 -1 "acgtBDHKMNRSVWY"B
          
        tasks.[(3*n-1)/blockSize+1] <-
            "\n>THREE Homo sapiens frequency\n"B |> fun i -> Task.FromResult(i,i.Length)

        cumsum [|0.3029549426680;0.1979883004921;0.1975473066391;0.3015094502008|]
        |> writeRandom (5*n) ((3*n-1)/blockSize+2) 0 "acgt"B
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
    "\n>TWO IUB ambiguity codes\n"B |> fun i -> out.Write(i,0,i.Length)

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

    0