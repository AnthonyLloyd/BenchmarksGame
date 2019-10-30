// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// Port to F# by Jomo Fisher of the C#
// Port of the C version by Anthony Lloyd

module PiDigitOld

open System
open System.Runtime.InteropServices

[<Struct;StructLayout(LayoutKind.Sequential)>]
type MPZ =
    val alloc: int
    val size: int
    val ptr: IntPtr

[<DllImport("gmp.so.10",EntryPoint="__gmpz_init",ExactSpelling=true)>]
extern void private mpz_init(MPZ& _value)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_init_set_ui",ExactSpelling=true)>]
extern void private mpz_init_set_ui(MPZ& _dest, uint32 _value)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_mul_ui",ExactSpelling=true)>]
extern void private mpz_mul_ui(MPZ& _dest, MPZ& _src, uint32 _value)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_add",ExactSpelling=true)>]
extern void private mpz_add(MPZ& _dest, MPZ& _src, MPZ& _src2)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_tdiv_q",ExactSpelling=true)>]
extern void private mpz_tdiv_q(MPZ& _dest, MPZ& _src, MPZ& _src2)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_get_ui",ExactSpelling=true)>]
extern uint32 private mpz_get_ui(MPZ& _src)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_submul_ui",ExactSpelling=true)>]
extern void private mpz_submul_ui(MPZ& _dest, MPZ& _src, uint32 _value)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_addmul_ui",ExactSpelling=true)>]
extern void private mpz_addmul_ui(MPZ& _dest, MPZ& _src, uint32 _value)

[<DllImport("gmp.so.10",EntryPoint="__gmpz_cmp",ExactSpelling=true)>]
extern int private mpz_cmp(MPZ& _op1, MPZ& _op2)

//[<EntryPoint>]
let main (args:string[]) =

    let mutable tmp1, tmp2, acc, den, num = MPZ(), MPZ(), MPZ(), MPZ(), MPZ()
    
    mpz_init(&tmp1)
    mpz_init(&tmp2)
    mpz_init_set_ui(&acc, 0u)
    mpz_init_set_ui(&den, 1u)
    mpz_init_set_ui(&num, 1u)
    
    let extract_digit nth =
        mpz_mul_ui(&tmp1, &num, nth)
        mpz_add(&tmp2, &tmp1, &acc)
        mpz_tdiv_q(&tmp1, &tmp2, &den)
        mpz_get_ui(&tmp1)

    let eliminate_digit d =
        mpz_submul_ui(&acc, &den, d)
        mpz_mul_ui(&acc, &acc, 10u)
        mpz_mul_ui(&num, &num, 10u)

    let next_term k =
        let k2 = k * 2u + 1u
        mpz_addmul_ui(&acc, &num, 2u)
        mpz_mul_ui(&acc, &acc, k2)
        mpz_mul_ui(&den, &den, k2)
        mpz_mul_ui(&num, &num, k)

    let out = new IO.MemoryStream()//Console.OpenStandardOutput()
    let bytes = Array.create 23 '\n'B
    bytes.[10] <- '\t'B; bytes.[11] <- ':'B; bytes.[13] <- '0'B
    let n = int args.[0]
    let mutable i, j, k, z = 0, 0, 0u, 1
    while i < n do
        k <- k + 1u
        next_term k
        if mpz_cmp(&num, &acc) <= 0 then
            let d = extract_digit 3u
            if d = extract_digit 4u then
                bytes.[j] <- byte d + 48uy
                j <- j + 1
                if j = 10 then
                    j <- 0
                    i <- i + 10
                    let rec setInt i p =
                        let i,r = Math.DivRem(i, 10)
                        bytes.[p] <- byte(r + 48)
                        if r=0 then
                            if p=12 then
                                bytes.[12] <- '1'B
                                z <- z + 1
                                for k = 1 to z do
                                    bytes.[12+k] <- '0'B
                            else setInt i (p-1)
                    setInt (i/10) (11+z)
                    out.Write(bytes, 0, 14+z)
                eliminate_digit d
    out.ToArray()