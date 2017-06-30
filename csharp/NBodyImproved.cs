/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, optimization and use of more C# idioms by Robert F. Tobler
   small optimizations by Anthony Lloyd
*/

using System;
using System.Numerics;

class Body { public Vector<double> X, V; public double Mass; }
class Pair { public Body bi, bj; }

public static class NBodyImproved
{
    const double dt = 0.01;
    static Body[] createBodies()
    {
        const double Pi = 3.141592653589793;
        const double Solarmass = 4 * Pi * Pi;
        const double DaysPeryear = 365.24;
        //Console.WriteLine(Vector<double>.Count);
        //Console.WriteLine("Hardware acc: " + Vector.IsHardwareAccelerated);
        var jupiter = new Body {
            
            X = new Vector<double>(new double[] { 4.84143144246472090e+00
                                                , -1.16032004402742839e+00
                                                , -1.03622044471123109e-01
                                                , 0 }),
            V = new Vector<double>(new double[] { 1.66007664274403694e-03 * DaysPeryear
                                                , 7.69901118419740425e-03 * DaysPeryear
                                                , -6.90460016972063023e-05 * DaysPeryear
                                                , 0 }),
            Mass = 9.54791938424326609e-04 * Solarmass,
        };
        var saturn = new Body {
            X = new Vector<double>(new double[] { 8.34336671824457987e+00
                                                , 4.12479856412430479e+00
                                                , -4.03523417114321381e-01
                                                , 0 }),
            V = new Vector<double>(new double[] { -2.76742510726862411e-03 * DaysPeryear
                                                , 4.99852801234917238e-03 * DaysPeryear
                                                , 2.30417297573763929e-05 * DaysPeryear
                                                , 0 }),
            Mass = 2.85885980666130812e-04 * Solarmass,
        };
        var uranus = new Body {
            X = new Vector<double>(new double[] { 1.28943695621391310e+01
                                                , -1.51111514016986312e+01
                                                , -2.23307578892655734e-01
                                                , 0 }),
            V = new Vector<double>(new double[] { 2.96460137564761618e-03 * DaysPeryear
                                                , 2.37847173959480950e-03 * DaysPeryear
                                                , -2.96589568540237556e-05 * DaysPeryear
                                                , 0 }),
            Mass = 4.36624404335156298e-05 * Solarmass,
        };
        var neptune = new Body {
            X = new Vector<double>(new double[] { 1.53796971148509165e+01
                                                , -2.59193146099879641e+01
                                                , 1.79258772950371181e-01
                                                , 0 }),
            V = new Vector<double>(new double[] { 2.68067772490389322e-03 * DaysPeryear
                                                , 1.62824170038242295e-03 * DaysPeryear
                                                , -9.51592254519715870e-05 * DaysPeryear
                                                , 0 }),
            Mass = 5.15138902046611451e-05 * Solarmass,
        };
        var sun = new Body {
                V = (jupiter.V * jupiter.Mass
                    + saturn.V * saturn.Mass
                    + uranus.V * uranus.Mass
                    + neptune.V * neptune.Mass
                    ) * (-1.0 / Solarmass),
                Mass = Solarmass,
            };
        return new Body[] {sun, jupiter, saturn, uranus, neptune};
    }

    static Pair[] createPairs(Body[] bodies)
    {
        var pairs = new Pair[bodies.Length * (bodies.Length-1)/2];
        int pi = 0;
        for (int i = 0; i < bodies.Length-1; i++)
            for (int j = i+1; j < bodies.Length; j++)
                pairs[pi++] = new Pair { bi = bodies[i], bj = bodies[j] };
        return pairs;
    }

    static double energy(Body[] bodies, Pair[] pairs)
    {
        double e = 0.0;
        foreach (var b in bodies)
        {
            e += b.Mass * Vector.Dot(b.V, b.V);
        }
        e *= 0.5;
        foreach (var p in pairs)
        {
            Body bi = p.bi, bj = p.bj;
            var dx = bi.X - bj.X;
            e -= (bi.Mass * bj.Mass) / Math.Sqrt(Vector.Dot(dx, dx));
        }
        return e;
    }

    public static void Main(String[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 10000;
        var bodies = createBodies();
        var pairs = createPairs(bodies);
        Console.WriteLine(energy(bodies, pairs).ToString("f9"));
        for(int i=0; i<n; i++) advance(pairs, bodies);
        Console.WriteLine(energy(bodies, pairs).ToString("f9"));
    }

    static void advance(Pair[] pairs, Body[] bodies)
    {
        foreach (var p in pairs)
        {
            Body bi = p.bi, bj = p.bj;
            var dx = bj.X - bi.X;
            double d2 = Vector.Dot(dx, dx);
            double mag = dt / (d2 * Math.Sqrt(d2));
            bi.V += bj.Mass * mag * dx;
            bj.V -= bi.Mass * mag * dx;
        }
        foreach (var b in bodies)
        {
            b.X += b.V * dt;
        }
    }

    public static double Test(String[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 10000;
        var bodies = createBodies();
        var pairs = createPairs(bodies);
        var energyBefore = energy(bodies, pairs);
        for(int i=0; i<n; i++) advance(pairs, bodies);
        return Math.Round(energyBefore,10) + Math.Round(energy(bodies, pairs),10);
    }
}