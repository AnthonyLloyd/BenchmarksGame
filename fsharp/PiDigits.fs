(**
 * The Computer Language Benchmarks Game
 * http://benchmarksgame.alioth.debian.org/
 *
 * Port to F# by Jomo Fisher of the C# port that uses native GMP:
 * 	contributed by Mike Pall
 * 	java port by Stefan Krause
 *  C# port by Miguel de Icaza
*)

open System.Runtime.InteropServices

[<Struct; StructLayout(LayoutKind.Sequential)>]
type MPZ =
   val _mp_alloc:int
   val _mp_size:int
   val ptr:System.IntPtr

[<DllImport ("gmp", EntryPoint="__gmpz_init",ExactSpelling=true,SetLastError=false)>]
extern void mpzInit(MPZ& _value)

[<DllImport ("gmp", EntryPoint="__gmpz_mul_si",ExactSpelling=true,SetLastError=false)>]
extern void mpzMul(MPZ& _dest, MPZ&_src, int _value)

[<DllImport ("gmp", EntryPoint="__gmpz_add",ExactSpelling=true,SetLastError=false)>]
extern void mpzAdd(MPZ& _dest, MPZ& _src, MPZ& _src2)

[<DllImport ("gmp", EntryPoint="__gmpz_tdiv_q",ExactSpelling=true,SetLastError=false)>]
extern void mpzTdiv(MPZ& _dest, MPZ& _src, MPZ& _src2)

[<DllImport ("gmp", EntryPoint="__gmpz_set_si",ExactSpelling=true,SetLastError=false)>]
extern void mpzSet(MPZ& _src, int _value)

[<DllImport ("gmp", EntryPoint="__gmpz_get_si",ExactSpelling=true,SetLastError=false)>]
extern int mpzGet(MPZ& _src)

[<EntryPoint>]
let main args =

    let inline init() =
        let mutable result = MPZ()
        mpzInit(&result)
        result

    let mutable q = init()
    let mutable r = init()
    let mutable s = init()
    let mutable t = init()
    let mutable u = init()
    let mutable v = init()
    let mutable w = init()

    mpzSet(&q, 1)
    mpzSet(&r, 0)
    mpzSet(&s, 0)
    mpzSet(&t, 1)

    let inline composeR bq br bs bt =
        mpzMul(&u, &r, bs)
        mpzMul(&r, &r, bq)
        mpzMul(&v, &t, br)
        mpzAdd(&r, &r, &v)
        mpzMul(&t, &t, bt)
        mpzAdd(&t, &t, &u)
        mpzMul(&s, &s, bt)
        mpzMul(&u, &q, bs)
        mpzAdd(&s, &s, &u)
        mpzMul(&q, &q, bq)

    let inline composeL bq br bs bt =
        mpzMul(&r, &r, bt)
        mpzMul(&u, &q, br)
        mpzAdd(&r, &r, &u)
        mpzMul(&u, &t, bs)
        mpzMul(&t, &t, bt)
        mpzMul(&v, &s, br)
        mpzAdd(&t, &t, &v)
        mpzMul(&s, &s, bq)
        mpzAdd(&s, &s, &u)
        mpzMul(&q, &q, bq)

    let inline extract j = 
        mpzMul(&u, &q, j)
        mpzAdd(&u, &u, &r)
        mpzMul(&v, &s, j)
        mpzAdd(&v, &v, &t)
        mpzTdiv(&w, &u, &v)
        mpzGet(&w)

    let bytes = Array.zeroCreate 10
    let n = int args.[0]
    let mutable i = 0
    let mutable c = 0
    let mutable k = 1
    let mutable more = true
    while more do
        let y = extract 3
        if y = extract 4 then
            bytes.[c] <- byte(48+y)
            c <- c + 1
            i <- i + 1
            if i%10=0 || i=n then
                while c<>10 do
                    bytes.[c] <- ' 'B
                    c<-c+1
                c <- 0
            stdout.Write [|0uy|]//Write ch
            stdout.Write "\t:"
            stdout.WriteLine i
            if i=n then more<-false
            else composeR 10 (-10*y) 0 1
        else
            composeL k (4*k+2) 0 (2*k+1)
            k<-k+1

    0