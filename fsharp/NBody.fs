// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// contributed by Valentin Kraevskiy
// modified by Peter Kese
// Use hardware intrinsics by Anthony Lloyd

open System.Runtime.Intrinsics
open System.Runtime.Intrinsics.X86

type Body =
    {mutable XY: Vector128<float>; mutable Z: float
     mutable VXY: Vector128<float>; mutable VZ: float
     Mass: float}

[<Literal>]
let dt = 0.01
[<Literal>]
let pi = 3.141592653589793
[<Literal>]
let daysPerYear = 365.24
[<Literal>]
let solarMass = 39.4784176043574 //4.0 * pi ** 2.0

[<EntryPoint>]
let main (args:string[]) =

    let inline scalar2 (a:float) = Vector128.Create(a, a)
    let inline sumSqr (xy:Vector128<float>) (z:float) =
        let xy2 = Ssse3.Multiply(xy, xy)
        xy2.ToScalar() + xy2.GetElement(1) + z*z

    let bodies =

        let jupiter = {
            XY = Vector128.Create(4.84143144246472090e+00
                                ,-1.16032004402742839e+00)
            Z = -1.03622044471123109e-01
            VXY = Vector128.Create(1.66007664274403694e-03 * daysPerYear
                                  ,7.69901118419740425e-03 * daysPerYear)
            VZ = -6.90460016972063023e-05 * daysPerYear
            Mass = 9.54791938424326609e-04 * solarMass }
        
        let saturn = {
            XY = Vector128.Create(8.34336671824457987e+00
                                 ,4.12479856412430479e+00)
            Z = -4.03523417114321381e-01
            VXY = Vector128.Create(-2.76742510726862411e-03 * daysPerYear
                                   ,4.99852801234917238e-03 * daysPerYear)
            VZ = 2.30417297573763929e-05 * daysPerYear
            Mass = 2.85885980666130812e-04 * solarMass }
        
        let uranus = {
            XY = Vector128.Create(1.28943695621391310e+01
                                ,-1.51111514016986312e+01)
            Z = -2.23307578892655734e-01
            VXY = Vector128.Create(2.96460137564761618e-03 * daysPerYear
                                  ,2.37847173959480950e-03 * daysPerYear)
            VZ = -2.96589568540237556e-05 * daysPerYear
            Mass = 4.36624404335156298e-05 * solarMass }
        
        let neptune = {
            XY = Vector128.Create(1.53796971148509165e+01
                                ,-2.59193146099879641e+01)
            Z = 1.79258772950371181e-01
            VXY = Vector128.Create(2.68067772490389322e-03 * daysPerYear
                                  ,1.62824170038242295e-03 * daysPerYear)
            VZ = -9.51592254519715870e-05 * daysPerYear
            Mass = 5.15138902046611451e-05 * solarMass }

        let bodies = [|Unchecked.defaultof<_>;jupiter;saturn;uranus;neptune|]
        
        bodies.[0] <-
            let mutable svxy, svz = Vector128.Zero, 0.0
            for i = 1 to 4 do
                let body = bodies.[i]
                let c = body.Mass / solarMass
                svxy <- Ssse3.Subtract(svxy,Ssse3.Multiply(body.VXY,scalar2 c))
                svz <- svz - c * body.VZ
            { XY = Vector128.Zero; Z=0.0; VXY=svxy; VZ=svz; Mass=solarMass }
        
        bodies

    let pairs = [|
        for i = 0 to 4 do
            for j = i+1 to 4 do
                bodies.[i], bodies.[j]
    |]

    let energy() =
        let ePlanets = Array.sumBy (fun b -> sumSqr b.VXY b.VZ * b.Mass) bodies
                            * 0.5
        let ePairs = Array.sumBy (fun (b1,b2) ->
            let sumSq2 = sumSqr (Ssse3.Subtract(b1.XY,b2.XY)) (b1.Z-b2.Z)
            b1.Mass * b2.Mass / sqrt sumSq2) pairs
        ePlanets - ePairs

    energy() |> printf "%.9f\n"

    let dtV = scalar2 dt

    for _ = 1 to int args.[0] do
        // calculate pairwise forces
        for b1,b2 in pairs do
            let dxy,dz = Ssse3.Subtract(b1.XY,b2.XY), b1.Z-b2.Z
            let dist2 = sumSqr dxy dz
            let mag = dt / (dist2 * sqrt dist2)
            let mag2 = -b2.Mass * mag
            b1.VXY <- Ssse3.Add(b1.VXY, Ssse3.Multiply(dxy, scalar2 mag2))
            b1.VZ <- b1.VZ + mag2 * dz
            let mag1 = b1.Mass * mag
            b2.VXY <- Ssse3.Add(b2.VXY, Ssse3.Multiply(dxy, scalar2 mag1))
            b2.VZ <- b2.VZ + mag1 * dz
        // move bodies
        for body in bodies do
            body.XY <- Ssse3.Add(body.XY, Ssse3.Multiply(body.VXY, dtV))
            body.Z <- body.Z + body.VZ * dt

    energy() |> printf "%.9f\n"

    0