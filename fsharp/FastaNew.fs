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
let LinesPerBlock = 2048 // 2048 // 16384

open System

type private Pool<'a> private () =
    static let buffer = Array.zeroCreate<Memory<'a>> 128
    static let mutable count = 0
    static let mutable lock = Threading.SpinLock false
    static member Rent() =
        let lockTaken = ref false
        let mutable bs = Unchecked.defaultof<_>
        lock.Enter lockTaken
        if count < buffer.Length then
            bs <- buffer.[count]
            count <- count + 1
        if !lockTaken then lock.Exit false
        if bs = Unchecked.defaultof<_> then
            Memory(Array.zeroCreate (Width1*LinesPerBlock))
        else bs
    static member Return a =
        let lockTaken = ref false
        lock.Enter lockTaken
        if count <> 0 then
            count <- count - 1
            buffer.[count] <- a
        if !lockTaken then lock.Exit false

//[<EntryPoint>]
let main (args:string []) =
    let n = if args.Length=0 then 1000 else Int32.Parse(args.[0])
    let out = new IO.MemoryStream()//Console.OpenStandardOutput()
    let tasks = (3*n-1)/(Width*LinesPerBlock)+(5*n-1)/(Width*LinesPerBlock)+3
                |> Array.zeroCreate
    Threading.ThreadPool.QueueUserWorkItem(fun _ ->

        let writeRandom n offset seed (vs:byte[]) (ps:float[]) =
            // cumulative probability
            let mutable total = ps.[0]
            for i = 1 to ps.Length-1 do
                total <- total + ps.[i]
                ps.[i] <- total
            
            let mutable seed = seed
            let inline rnds l j =
                let memory = Pool.Rent()
                let span = memory.Span.Slice(0,l+1)
                for i = 0 to span.Length-2 do
                    seed <- (seed * 3877 + 29573) % 139968
                    span.[i] <- seed
                span.[l] <- j
                memory
            let inline bytes l (rnds:Memory<int>) =
                let memory = Pool.Rent()
                let inline lookup probability =
                    let rec search i =
                        if ps.[i]>=probability then i
                        else search (i+1)
                    vs.[search 0]
                let byteSpan = memory.Span
                let intSpan = rnds.Span
                for i = 0 to l-1 do
                    byteSpan.[1+i+i/Width] <- 1.0/139968.0 * float intSpan.[i] |> lookup
                Pool.Return rnds
                for i = 0 to (l-1)/Width do
                    byteSpan.[i*Width1] <- '\n'B
                memory

            let createBytes =
                let bytes (o:obj) =
                    let rnds = o :?> Memory<int>
                    tasks.[rnds.Span.[Width*LinesPerBlock]] <-
                        bytes (Width*LinesPerBlock) rnds
                Threading.WaitCallback bytes

            for i = offset to offset+(n-1)/(Width*LinesPerBlock)-1 do
                let rnds = rnds (Width*LinesPerBlock) i
                Threading.ThreadPool.QueueUserWorkItem(createBytes, rnds) |> ignore
                
            let remaining = (n-1)%(Width*LinesPerBlock)+1
            let rnds = rnds remaining (offset+(n-1)/(Width*LinesPerBlock))
            Threading.ThreadPool.QueueUserWorkItem(fun o ->
                let rnds = o :?> Memory<int>
                tasks.[rnds.Span.[remaining]] <-
                    let bytes = bytes remaining rnds
                    bytes.Span.[remaining+(remaining-1)/Width+1] <- '\n'B
                    bytes.Slice(0, remaining+(remaining-1)/Width+2)
            , rnds) |> ignore
            seed 
    
        let seed =
            [|0.27;0.12;0.12;0.27;0.02;0.02;0.02;
              0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02|]
            |> writeRandom (3*n) 0 42 "acgtBDHKMNRSVWY"B

        [|0.3029549426680;0.1979883004921;0.1975473066391;0.3015094502008|]
        |> writeRandom (5*n) ((3*n-1)/(Width*LinesPerBlock)+2) seed "acgt"B
        |> ignore
    , null) |> ignore

    out.Write(">ONE Homo sapiens alu"B,0,21)
    let table =
        "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG\
         GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA\
         CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT\
         ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA\
         GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG\
         AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC\
         AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA"B
    let repeatedBytes = Pool.Rent()
    let repeatedBytesSpan = repeatedBytes.Span
    let linesPerBlock = (LinesPerBlock/287-1) * 287
    for i = 0 to Width*linesPerBlock-1 do
        repeatedBytesSpan.[1+i+i/Width] <- table.[i%287]
    for i = 0 to (Width*linesPerBlock-1)/Width do
        repeatedBytesSpan.[i*Width1] <- '\n'B
    let roSpan = (Memory.op_Implicit repeatedBytes).Span.Slice(0, Width1*linesPerBlock)
    for _ = 1 to (2*n-1)/(Width*linesPerBlock) do
        out.Write roSpan
    let remaining = (2*n-1)%(Width*linesPerBlock)+1
    out.Write (roSpan.Slice(0, remaining+(remaining-1)/Width+1))
    Pool.Return repeatedBytes
    out.Write("\n>TWO IUB ambiguity codes"B,0,25)

    tasks.[(3*n-1)/(Width*LinesPerBlock)+1] <-
        Memory(">THREE Homo sapiens frequency"B)

    for i = 0 to tasks.Length-1 do
        let mutable t = tasks.[i]
        while t = Unchecked.defaultof<_> do
            Threading.Thread.Sleep 0
            t <- tasks.[i]
        out.Write (Memory.op_Implicit t).Span
        if t.Length=Width1*LinesPerBlock then Pool.Return t

    out.ToArray()