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

    let oneTask = Task.Run (fun () ->
        let one = ">ONE Homo sapiens alu\n"B
        out.Write(one,0,one.Length)
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
        let two = "\n>TWO IUB ambiguity codes\n"B
        out.Write(two,0,two.Length)
    )

    let intPool = System.Buffers.ArrayPool.Shared
    let im,ia,ic = 139968,3877,29573
    let oneOverIM = 1.0/139968.0
    let mutable seed = 42
    let inline rnds l =
        lock intPool (fun () ->
            let a = intPool.Rent l
            for i = 0 to l-1 do
                seed <- (seed * ia + ic) % im
                a.[i] <- seed
            a
        )
    let tasks = ResizeArray(n*8/(Width*LinesPerBlock)+5)

    let writeRandom n d (vs:byte[]) (ps:float[]) =
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
        let l = Width*LinesPerBlock
        for __ = 1 to (n-1)/l do
            tasks.Add(Task.Run(fun () ->
                let rnds = rnds l
                bytes l 0 rnds, l+(l+d)/Width
            ))
        let remaining = (n-1)%l+1
        if remaining<>0 then
            let rnds = rnds remaining
            tasks.Add(Task.Run(fun () ->
                bytes remaining d rnds, remaining+(remaining+d)/Width
            ))

    let cumsum (ps:float[]) =
        let mutable total = ps.[0]
        for i = 1 to ps.Length-1 do
            total <- total + ps.[i]
            ps.[i] <- total
        ps

    cumsum [|0.27;0.12;0.12;0.27;0.02;0.02;0.02;
             0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02|]
    |> writeRandom (3*n) -1 "acgtBDHKMNRSVWY"B
      

    "\n>THREE Homo sapiens frequency\n"B |> fun i ->
        tasks.Add(Task.FromResult(i,i.Length))

    cumsum [|0.3029549426680;0.1979883004921;0.1975473066391;0.3015094502008|]
    |> writeRandom (5*n) 0 "acgt"B

    oneTask.Wait()

    tasks |> Seq.iter (fun t ->
        let bs,l = t.Result
        out.Write(bs,0,l)
        if l>200 then bytePool.Return bs
    )

    0