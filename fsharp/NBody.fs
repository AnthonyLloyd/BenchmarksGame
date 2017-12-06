// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// ported from C# version by Anthony Lloyd

#nowarn "9"

[<Literal>]
let N = 5
[<Literal>]
let dt = 0.01
[<Literal>]
let Pi = 3.141592653589793
[<Literal>]
let DaysPeryear = 365.24
let Solarmass = 4.0 * 3.141592653589793 * 3.141592653589793
[<Literal>]
let Jx = 4.84143144246472090e+00
[<Literal>]
let Jy = -1.16032004402742839e+00
[<Literal>]
let Jz = -1.03622044471123109e-01
[<Literal>]
let Jvx = 1.66007664274403694e-03
[<Literal>]
let Jvy = 7.69901118419740425e-03
[<Literal>]
let Jvz = -6.90460016972063023e-05
[<Literal>]
let Jmass = 9.54791938424326609e-04
[<Literal>]
let Sx = 8.34336671824457987e+00
[<Literal>]
let Sy = 4.12479856412430479e+00
[<Literal>]
let Sz = -4.03523417114321381e-01
[<Literal>]
let Svx = -2.76742510726862411e-03
[<Literal>]
let Svy = 4.99852801234917238e-03
[<Literal>]
let Svz = 2.30417297573763929e-05
[<Literal>]
let Smass = 2.85885980666130812e-04
[<Literal>]
let Ux = 1.28943695621391310e+01
[<Literal>]
let Uy = -1.51111514016986312e+01
[<Literal>]
let Uz = -2.23307578892655734e-01
[<Literal>]
let Uvx = 2.96460137564761618e-03
[<Literal>]
let Uvy = 2.37847173959480950e-03
[<Literal>]
let Uvz = -2.96589568540237556e-05
[<Literal>]
let Umass = 4.36624404335156298e-05
[<Literal>]
let Nx = 1.53796971148509165e+01
[<Literal>]
let Ny = -2.59193146099879641e+01
[<Literal>]
let Nz = 1.79258772950371181e-01
[<Literal>]
let Nvx = 2.68067772490389322e-03
[<Literal>]
let Nvy = 1.62824170038242295e-03
[<Literal>]
let Nvz = -9.51592254519715870e-05
[<Literal>]
let Nmass = 5.15138902046611451e-05

[<Struct>]
type NBody =
      val mutable X: float
      val mutable Y: float
      val mutable Z: float
      val mutable VX: float
      val mutable VY: float
      val mutable VZ: float
      val mutable Mass : float
      new(x,y,z,vx,vy,vz,mass) = {X=x;Y=y;Z=z;VX=vx;VY=vy;VZ=vz;Mass=mass}

open Microsoft.FSharp.NativeInterop

[<EntryPoint>]
let main args =

    let ptrBody = NativePtr.stackalloc<NBody> N

    // sun
    NBody (
        0.0,
        0.0,
        0.0,
        (Jvx * Jmass + Svx * Smass + Uvx * Umass + Nvx * Nmass) * -DaysPeryear,
        (Jvy * Jmass + Svy * Smass + Uvy * Umass + Nvy * Nmass) * -DaysPeryear,
        (Jvz * Jmass + Svz * Smass + Uvz * Umass + Nvz * Nmass) * -DaysPeryear,
        Solarmass
    ) |> NativePtr.set ptrBody 0

    NBody ( // jupiter
        Jx,
        Jy,
        Jz,
        Jvx * DaysPeryear,
        Jvy * DaysPeryear,
        Jvz * DaysPeryear,
        Jmass * Solarmass
     ) |> NativePtr.set ptrBody 1

    NBody ( // saturn
        Sx,
        Sy,
        Sz,
        Svx * DaysPeryear,
        Svy * DaysPeryear,
        Svz * DaysPeryear,
        Smass * Solarmass
    ) |> NativePtr.set ptrBody 2

    NBody ( // uranus
        Ux,
        Uy,
        Uz,
        Uvx * DaysPeryear,
        Uvy * DaysPeryear,
        Uvz * DaysPeryear,
        Umass * Solarmass
    ) |> NativePtr.set ptrBody 3

    NBody ( // neptune
        Nx,
        Ny,
        Nz,
        Nvx * DaysPeryear,
        Nvy * DaysPeryear,
        Nvz * DaysPeryear,
        Nmass * Solarmass
    ) |> NativePtr.set ptrBody 4

    let inline sqr x = x * x

    let inline energy() =
        let mutable e = 0.0
        for i = 0 to N-1 do
            let bi = NativePtr.get ptrBody i
            e <- e + 0.5 * bi.Mass * (sqr bi.VX + sqr bi.VY + sqr bi.VZ)
            for j = i+1 to N-1 do
                let bj = NativePtr.get ptrBody j
                e <- e - bi.Mass * bj.Mass / sqrt(sqr(bi.X-bj.X) + sqr(bi.Y-bj.Y) + sqr(bi.Z-bj.Z))
        e

    let inline getD2 dx dy dz =
        let d2 = sqr dx + sqr dy + sqr dz
        d2 * sqrt d2

    energy().ToString("F9") |> stdout.WriteLine

    let mutable advancements = if args.Length=0 then 1000 else int args.[0]
    while (advancements <- advancements - 1; advancements>=0) do
        for i = 0 to N-1 do
            let mutable bi = NativePtr.get ptrBody i
            for j = i+1 to N-1 do
                let mutable bj = NativePtr.get ptrBody j
                let dx = bj.X - bi.X
                let dy = bj.Y - bi.Y
                let dz = bj.Z - bi.Z
                let mag = dt / getD2 dx dy dz
                bj.VX <- bj.VX - dx * bi.Mass * mag
                bj.VY <- bj.VY - dy * bi.Mass * mag
                bj.VZ <- bj.VZ - dz * bi.Mass * mag
                NativePtr.set ptrBody j bj
                bi.VX <- bi.VX + dx * bj.Mass * mag
                bi.VY <- bi.VY + dy * bj.Mass * mag
                bi.VZ <- bi.VZ + dz * bj.Mass * mag
            bi.X <- bi.X + bi.VX * dt
            bi.Y <- bi.Y + bi.VY * dt
            bi.Z <- bi.Z + bi.VZ * dt
            NativePtr.set ptrBody i bi

    energy().ToString("F9") |> stdout.WriteLine

    0