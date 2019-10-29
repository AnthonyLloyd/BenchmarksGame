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

    let n = uint32 args.[0]
    let out = new System.IO.MemoryStream()//System.Console.OpenStandardOutput()
    let encoding = System.Text.Encoding.ASCII
    let mutable i, k = 0u, 0u
    while i < n do
        k <- k + 1u
        next_term k
        if mpz_cmp(&num, &acc) <= 0 then
            let d = extract_digit 3u
            if d = extract_digit 4u then
                out.WriteByte(byte(d+48u))
                i <- i + 1u
                if i % 10u = 0u then
                    let bs = encoding.GetBytes("\t:" + string i + "\n")
                    out.Write(bs, 0, bs.Length)
                eliminate_digit d
    out.ToArray()