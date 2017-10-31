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
    fun () ->
        let p = rnd()
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
let Lines = 8


[<EntryPoint>]
let main args =
    let n = if args.Length=0 then 1000 else int args.[0]
    let out = System.Console.OpenStandardOutput()
    let inline writeSub (bs:byte[]) l = out.Write(bs,0,l)
    let inline write bs = writeSub bs bs.Length

    let writeRepeat n (table:byte[]) =
        let tableLength = table.Length
        let repeatedBytesLength = tableLength*Width1
        let repeatedBytes =
            Array.init repeatedBytesLength
              (fun i -> if i%Width1=0 then '\n'B else table.[(i-1-i/Width1)%tableLength])
        for __ = 1 to n/(tableLength*Width) do write repeatedBytes
        let remaining = n%(tableLength*Width)
        if remaining<>0 then writeSub repeatedBytes (1+remaining+(remaining-1)/Width)

    let writeRandom n gen =
        let length = Width1*Lines
        let bytes l =
            Array.init l
              (fun i -> if i%Width1=0 then '\n'B else gen())
        for __ = 1 to n/(Width*Lines) do bytes length |> write
        let remaining = n%(Width*Lines)
        if remaining<>0 then bytes (1+remaining+(remaining-1)/Width) |> write

    write ">ONE Homo sapiens alu"B
    
    writeRepeat (2*n)
        "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG\
         GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA\
         CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT\
         ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA\
         GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG\
         AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC\
         AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA"B

    write "\n>TWO IUB ambiguity codes"B

    createGenerator "acgtBDHKMNRSVWY"B
        [|0.27;0.12;0.12;0.27;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02;0.02|]
    |> writeRandom (3*n)

    write "\n>THREE Homo sapiens frequency"B

    createGenerator "acgt"B 
        [|0.3029549426680;0.1979883004921;0.1975473066391;0.3015094502008|]
    |> writeRandom (5*n)

    out.WriteByte '\n'B
    
    0

// let rec search (lo,hi) =
//     if lo=hi then lo
//     else
//         let mid = lo+hi >>> 1
//         search (if ps.[mid]>=p then lo,mid else mid+1,hi)
// vs.[search (0,e)]