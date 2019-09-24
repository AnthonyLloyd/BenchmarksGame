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
let LinesPerBlock = 2048

[<Struct;NoEquality;NoComparison>]
type IO<'a> = IO of (('a -> unit) -> unit)

module IO =
    open System.Threading
    let run (IO run) : unit =
        use mre = new ManualResetEvent false
        ThreadPool.UnsafeQueueUserWorkItem (fun _ ->
            run (fun () -> mre.Set() |> ignore)
        ,null) |> ignore
        mre.WaitOne() |> ignore
    let fork (IO run) : IO<IO<'a>> =
        IO (fun contFork ->
            let mutable o = null
            ThreadPool.UnsafeQueueUserWorkItem (fun _ ->
                run (fun a ->
                    let o = Interlocked.CompareExchange(&o, a, null)
                    if isNull o |> not then (o :?> 'a->unit) a
                )
            ,null) |> ignore
            IO (fun cont ->
                let o = Interlocked.CompareExchange(&o, cont, null)
                if isNull o |> not then cont (o :?> 'a)
            ) |> contFork
        )

type IOBuilder() =
    member inline _.Bind(IO run, f:'a->IO<'b>) : IO<'b> =
        IO (fun cont ->
            run (fun o ->
                let (IO run) = f o
                run cont
            )
        )
    member inline _.Return value = IO (fun cont -> cont value)
    member inline _.Zero() = IO (fun cont -> cont Unchecked.defaultof<_>)

let io = IOBuilder()

open System

//[<EntryPoint>]
let main (args:string []) =
    let n = if args.Length=0 then 1000 else Int32.Parse(args.[0])
    let out = new IO.MemoryStream()//Console.OpenStandardOutput()
    let noTasks = (3*n-1)/(Width*LinesPerBlock)+(5*n-1)/(Width*LinesPerBlock)+3
    let randsIO = Array.zeroCreate noTasks
    let bytesIO = Array.zeroCreate noTasks
    let bytePool = Buffers.ArrayPool.Shared
    let intPool = Buffers.ArrayPool.Shared

    let rec writeRandom nn offset d seed (vs:byte[]) (ps:float[]) = io {
        do // cumulative probability
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
        let inline bytes l d (rnds:int[]) =
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

        let lastFullBlock = offset+(nn-1)/(Width*LinesPerBlock)-1
        let rec createBlock i = io {
            let rndsA = rnds (Width*LinesPerBlock)
            let! a = io { return bytes (Width*LinesPerBlock) 0 rndsA } |> IO.fork
            bytesIO.[i] <- a
            if i = lastFullBlock then
                let! r = io {
                    let remaining = (nn-1)%(Width*LinesPerBlock)+1
                    let rnds = rnds remaining
                    let! a = io { return bytes remaining d rnds } |> IO.fork
                    bytesIO.[i+1] <- a
                    if offset=0 then
                        let! next = 
                            writeRandom (5*n) ((3*n-1)/(Width*LinesPerBlock)+2) 0 seed "acgt"B
                                [|0.3029549426680;0.1979883004921;0.1975473066391;0.3015094502008|]
                            |> IO.fork                    
                        randsIO.[i+3] <- next
                        return 1
                    else
                        return 1 } |> IO.fork
                randsIO.[i+1] <- r
                return 1
            else
                let! r = createBlock (i+1) |> IO.fork
                randsIO.[i+1] <- r
                return 1
        }

        let! rr = createBlock offset

        return rr
    }

    let inline first() =
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
        for _ = 1 to (2*n-1)/(Width*linesPerBlock) do
            out.Write(repeatedBytes, 0, Width1*linesPerBlock)
        let remaining = (2*n-1)%(Width*linesPerBlock)+1
        if remaining<>0 then
            out.Write(repeatedBytes, 0, remaining+(remaining-1)/Width)
        bytePool.Return repeatedBytes
        out.Write("\n>TWO IUB ambiguity codes\n"B, 0, 26)

    io {

        let! r =
            writeRandom (3*n) 0 -1 42 "acgtBDHKMNRSVWY"B
                [|0.27;0.12;0.12;0.27;0.02;0.02;0.02;
                    0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02|]
            |> IO.fork
        randsIO.[0] <- r

        first()

        randsIO.[(3*n-1)/(Width*LinesPerBlock)+1] <-
            io { return 1 }
        bytesIO.[(3*n-1)/(Width*LinesPerBlock)+1] <-
            io { return "\n>THREE Homo sapiens frequency\n"B, 31 }

        let rec write i = io {
            if i < noTasks then
                let! _ =  randsIO.[i]
                let! bs,l = bytesIO.[i]
                out.Write(bs,0,l)
                do if l>200 then bytePool.Return bs
                do! write (i+1)
        }

        do! write 0

        out.WriteByte '\n'B

    } |> IO.run

    out.ToArray()