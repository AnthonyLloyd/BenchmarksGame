// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// contributed by Valentin Kraevskiy
// modified by Peter Kese

module NBodyNew

open System.Runtime.Intrinsics
open System.Runtime.Intrinsics.X86

[<Struct>]
type Vec3 = Vec3 of Vector128<float> * float with
    static member inline create(x:float,y:float,z:float) =
        Vec3(Vector128.Create(x,y),z)
    static member inline create(x:float) =
        Vec3(Vector128.Create(x,x),x)
    static member inline (+)((Vec3(v1,f1)),(Vec3(v2,f2))) =
        Vec3 (Sse3.Add(v1,v2), f1+f2)
    static member inline (-)((Vec3(v1,f1)),(Vec3(v2,f2))) =
        Vec3 (Sse3.Subtract(v1,v2), f1-f2)
    static member inline (*)((Vec3(v1,f1)),(Vec3(v2,f2))) =
        Vec3 (Sse3.Multiply(v1,v2), f1*f2)
    static member inline (*)((Vec3(v1,f1)),f2:float) =
        Vec3 (Sse3.Multiply(v1,Vector128.Create(f2,f2)), f1*f2)
    static member inline Zero = Vec3(Vector128.Create(0.0,0.0),0.0)
    static member inline sumSqr((Vec3(v,f))) =
        let v2 = Ssse3.Multiply(v, v)
        v2.ToScalar() + v2.GetElement(1) + f*f

type Body = { mutable X: Vec3; mutable V: Vec3; Mass: float}

[<Literal>]
let dt = 0.01
[<Literal>]
let pi = 3.141592653589793
[<Literal>]
let daysPerYear = 365.24
[<Literal>]
let solarMass = 39.4784176043574 //4.0 * pi ** 2.0

//[<EntryPoint>]
let main (args:string[]) =

    let bodies =

        let jupiter = {
            X = Vec3.create(4.84143144246472090e+00
                          ,-1.16032004402742839e+00
                          ,-1.03622044471123109e-01)
            V = Vec3.create(1.66007664274403694e-03 * daysPerYear
                           ,7.69901118419740425e-03 * daysPerYear
                          ,-6.90460016972063023e-05 * daysPerYear)
            Mass = 9.54791938424326609e-04 * solarMass }
        
        let saturn = {
            X = Vec3.create(8.34336671824457987e+00
                            ,4.12479856412430479e+00
                            ,-4.03523417114321381e-01)
            V = Vec3.create(-2.76742510726862411e-03 * daysPerYear
                            ,4.99852801234917238e-03 * daysPerYear
                            ,2.30417297573763929e-05 * daysPerYear)
            Mass = 2.85885980666130812e-04 * solarMass }
        
        let uranus = {
            X = Vec3.create(1.28943695621391310e+01
                          ,-1.51111514016986312e+01
                          ,-2.23307578892655734e-01)
            V = Vec3.create(2.96460137564761618e-03 * daysPerYear
                           ,2.37847173959480950e-03 * daysPerYear
                          ,-2.96589568540237556e-05 * daysPerYear)
            Mass = 4.36624404335156298e-05 * solarMass }
        
        let neptune = {
            X = Vec3.create(1.53796971148509165e+01
                          ,-2.59193146099879641e+01
                          ,1.79258772950371181e-01)
            V = Vec3.create(2.68067772490389322e-03 * daysPerYear
                           ,1.62824170038242295e-03 * daysPerYear
                          ,-9.51592254519715870e-05 * daysPerYear)
            Mass = 5.15138902046611451e-05 * solarMass }

        let bodies = [|Unchecked.defaultof<_>;jupiter;saturn;uranus;neptune|]
        
        bodies.[0] <-
            let mutable v = Vec3.Zero
            for i = 1 to 4 do
                let body = bodies.[i]
                v <- v - body.V * (body.Mass / solarMass)
            { X = Vec3.Zero; V = v; Mass=solarMass }
        
        bodies


    let pairs = [|
        for i = 0 to 4 do
            for j = i+1 to 4 do
                bodies.[i], bodies.[j]
    |]
    let energy() =
        let ePlanets = Array.sumBy (fun b -> Vec3.sumSqr b.V * b.Mass) bodies * 0.5
        let ePairs = Array.sumBy (fun (b1,b2) ->
            b1.Mass * b2.Mass / sqrt(Vec3.sumSqr(b1.X-b2.X))) pairs
        ePlanets - ePairs

    let bs = energy() |> sprintf "%.9f\n" //s

    let dtV = Vec3.create dt

    for _ = 1 to int args.[0] do
        // calculate pairwise forces
        for b1,b2 in pairs do
            let dx = b1.X - b2.X
            let dist2 = Vec3.sumSqr dx
            let mag = dt / (dist2 * sqrt dist2)
            b1.V <- b1.V - dx * Vec3.create(b2.Mass * mag)
            b2.V <- b2.V + dx * Vec3.create(b1.Mass * mag)
        // move bodies
        for body in bodies do
            body.X <- body.X + body.V * dtV

    let be = energy() |> sprintf "%.9f\n" //s

    bs,be //0