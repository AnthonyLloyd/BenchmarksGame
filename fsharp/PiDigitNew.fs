// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// Port to F# by Jomo Fisher of the C#
// Small optimisations by Anthony Lloyd

module PiDigitNew

#nowarn "9"

open System.Runtime.InteropServices

[<Struct;StructLayout(LayoutKind.Sequential)>]
type MPZ =
    val _alloc: int
    val _size: int
    val ptr: System.IntPtr

[<DllImport("gmp.so.10",EntryPoint="__gmpz_init",ExactSpelling=true,SetLastError=false)>]
extern void private mpzInit(MPZ& _value)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_mul_si",ExactSpelling=true,SetLastError=false)>]
extern void private mpzMul(MPZ& _dest, MPZ& _src, int _value)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_add",ExactSpelling=true,SetLastError=false)>]
extern void private mpzAdd(MPZ& _dest, MPZ& _src, MPZ& _src2)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_tdiv_q",ExactSpelling=true,SetLastError=false)>]
extern void private mpzTdiv(MPZ& _dest, MPZ& _src, MPZ& _src2)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_set_si",ExactSpelling=true,SetLastError=false)>]
extern void private mpzSet(MPZ& _src, int _value)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_get_si",ExactSpelling=true,SetLastError=false)>]
extern int private mpzGet(MPZ& _src)

//[<EntryPoint>]
let main (args:string[]) =

    let init() =
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

    let composeR bq br bs bt =
        let pr = PerfSimple.regionStart "composeR"
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
        PerfSimple.regionEnd pr

    let composeL bq br bs bt =
        let pr = PerfSimple.regionStart "composeL"
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
        PerfSimple.regionEnd pr

    let inline extract j =
        let pr = PerfSimple.regionStart "extract"
        mpzMul(&u, &q, j)
        mpzAdd(&u, &u, &r)
        mpzMul(&v, &s, j)
        mpzAdd(&v, &v, &t)
        mpzTdiv(&w, &u, &v)
        let r = mpzGet(&w)
        PerfSimple.regionEnd pr
        r

    let out = new System.IO.MemoryStream()//System.Console.OpenStandardOutput()
    let n = int args.[0]
    let bytes = Array.zeroCreate 18
    bytes.[10] <- '\t'B; bytes.[11] <- ':'B
    bytes.[14] <- '\n'B; bytes.[15] <- '\n'B
    bytes.[16] <- '\n'B; bytes.[17] <- '\n'B
    let encoding = System.Text.Encoding.ASCII
    let inline write i =
        let s = i.ToString()
        encoding.GetBytes(s, 0, s.Length, bytes, 12) |> ignore
        out.Write(bytes, 0, s.Length+13)
    let mutable i,j,k = 0,0,1
    while i<n do
        let y = extract 3
        if y = extract 4 then
            composeR 10 (y* -10) 0 1
            bytes.[j] <- byte(y+48)
            i<-i+1
            j<-j+1
            if j=10 then
                let writePR = PerfSimple.regionStart "write"
                j<-0
                write i
                PerfSimple.regionEnd writePR
        else
            composeL k (k*4+2) 0 (k*2+1)
            k<-k+1
    if j<>0 then
        let pr = PerfSimple.regionStart "no"
        for c = j to 9 do bytes.[c] <- ' 'B
        write n
        PerfSimple.regionEnd pr
    out.ToArray()