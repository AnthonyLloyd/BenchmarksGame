// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Contributed by Valentin Kraevskiy
let mutable seed = 42
let inline private rnd() =
    let im,ia,ic = 139968,3877,29573
    let imFloat = 139968.0
    seed <- (seed * ia + ic) % im
    float seed / imFloat

let inline private cumsum (a:float[]) =
    let mutable total = a.[0]
    for i = 1 to a.Length-1 do
        total <- total + a.[i]
        a.[i] <- total

let createGenerator (vs:byte[]) (ps:float[]) =
    cumsum ps
    let e = ps.Length-1
    fun p ->
        let rec search i =
            if i=e then e
            elif ps.[i]>=p then i
            else search (i+1)
        vs.[search 0]

[<Literal>]    
let Width = 60
[<Literal>]
let Width1 = 61
[<Literal>]
let LinesPerBlock = 1024

// type SpinLock() =
//     let s = System.Threading.SpinLock false
//     member __.Run f =
//         let lockTaken = ref false
//         try
//             s.Enter lockTaken
//             f()
//         finally
//             if !lockTaken then s.Exit()

[<EntryPoint>]
let main args =
    let n = if args.Length=0 then 1000 else int args.[0]
    let out = System.Console.OpenStandardOutput()//System.IO.Stream.Null//
    let inline writeSub (bs:byte[]) l = out.Write(bs,0,l)
    let inline write bs = writeSub bs bs.Length

    let inline writeRepeat n tableLength (table:byte[]) =
        let linesPerBlock = (LinesPerBlock/tableLength+1) * tableLength
        let repeatedBytes = Array.zeroCreate (Width1*linesPerBlock)
        for i = 0 to linesPerBlock*Width-1 do
            repeatedBytes.[i+i/Width] <- table.[i%tableLength]
        for i = 1 to linesPerBlock do
            repeatedBytes.[i*Width1-1] <- '\n'B
        for __ = 1 to (n-1)/(Width*linesPerBlock) do write repeatedBytes        
        let remaining = (n-1)%(Width*linesPerBlock)+1
        if remaining<>0 then
            writeSub repeatedBytes (remaining+(remaining-1)/Width)

    let writeRandom n d gen =
        let bytes l d =
            let a = Array.zeroCreate (l+(l+d)/Width)
            for i = 0 to l-1 do
                a.[i+i/Width] <- rnd() |> gen
            for i = 1 to (l+d)/Width do
                a.[i*Width1-1] <- '\n'B
            a            
        for __ = 1 to (n-1)/(Width*LinesPerBlock) do
            bytes (Width*LinesPerBlock) 0 |> write
        let remaining = (n-1)%(Width*LinesPerBlock)+1
        if remaining<>0 then bytes remaining d |> write

    write ">ONE Homo sapiens alu\n"B
    
    writeRepeat (2*n) 287
        "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG\
         GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA\
         CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT\
         ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA\
         GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG\
         AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC\
         AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA"B

    write "\n>TWO IUB ambiguity codes\n"B

    createGenerator "acgtBDHKMNRSVWY"B
        [|0.27;0.12;0.12;0.27;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02|]
    |> writeRandom (3*n) -1

    write "\n>THREE Homo sapiens frequency\n"B

    createGenerator "acgt"B 
        [|0.3029549426680;0.1979883004921;0.1975473066391;0.3015094502008|]
    |> writeRandom (5*n) 0
    
    0

// let rec search (lo,hi) =
//     if lo=hi then lo
//     else
//         let mid = lo+hi >>> 1
//         search (if ps.[mid]>=p then lo,mid else mid+1,hi)
// vs.[search (0,e)]