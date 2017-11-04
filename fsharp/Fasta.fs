// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Contributed by Valentin Kraevskiy

type SpinLock() =
  let s = System.Threading.SpinLock false
  member __.Run f =
    let lockTaken = ref false
    s.Enter lockTaken
    f()
    if !lockTaken then s.Exit()
let private rndsLock = SpinLock()
let mutable seed = 42
let inline private rnds (a:_[]) =
    rndsLock.Run(fun () ->
        let im,ia,ic = 139968,3877,29573
        for i = 0 to a.Length-1 do
            seed <- (seed * ia + ic) % im
            a.[i] <- seed
    )    
    a

let inline private cumsum (a:float[]) =
    let mutable total = a.[0]
    for i = 1 to a.Length-1 do
        total <- total + a.[i]
        a.[i] <- total * 139968.0
    a

let inline lookup (vs:byte[]) (ps:float[]) p =
    let rec search i =
        if ps.[i]>=p then i
        else search (i+1)
    vs.[search 0]

[<Literal>]    
let Width = 60
[<Literal>]
let Width1 = 61
[<Literal>]
let LinesPerBlock = 1024

[<EntryPoint>]
let main args =
    let n = if args.Length=0 then 1000 else System.Int32.Parse(args.[0])
    let out = System.IO.Stream.Null//System.Console.OpenStandardOutput()//
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

    let writeRandom n d vs ps =
        let bytes l d =
            let a = Array.zeroCreate (l+(l+d)/Width)
            let rnds = Array.zeroCreate l |> rnds
            for i = 0 to l-1 do
                a.[i+i/Width] <- float rnds.[i] |> lookup vs ps
            for i = 1 to (l+d)/Width do
                a.[i*Width1-1] <- '\n'B
            a            
        for __ = 1 to (n-1)/(Width*LinesPerBlock) do
            bytes (Width*LinesPerBlock) 0 |> write
        let remaining = (n-1)%(Width*LinesPerBlock)+1
        if remaining<>0 then bytes remaining d |> write

    // write ">ONE Homo sapiens alu\n"B
    
    // writeRepeat (2*n) 287
    //     "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG\
    //      GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA\
    //      CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT\
    //      ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA\
    //      GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG\
    //      AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC\
    //      AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA"B

    // write "\n>TWO IUB ambiguity codes\n"B

    [|0.27;0.12;0.12;0.27;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02|]
    |> cumsum
    |> writeRandom (3*n) -1 "acgtBDHKMNRSVWY"B

    // write "\n>THREE Homo sapiens frequency\n"B

    [|0.3029549426680;0.1979883004921;0.1975473066391;0.3015094502008|]
    |> cumsum
    |> writeRandom (5*n) 0 "acgt"B
    
    0

    // fixed size array pool (async?). seq<Task<byte[]*int*IDisposable>>. writer code easy.
    // how to ensure rnd are thread safe? just lock it 
