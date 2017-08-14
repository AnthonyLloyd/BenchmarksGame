/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, optimization and use of more C# idioms by Robert F. Tobler
   small loop and other optimisations by Anthony Lloyd
*/

using System;
using System.Runtime.CompilerServices;

public static class NBody2
{
    const double dt = 0.01;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double[,] createBodies()
    {
        const double Pi = 3.141592653589793;
        const double Solarmass = 4 * Pi * Pi;
        const double DaysPeryear = 365.24;
        const double jmass = 9.54791938424326609e-04 * Solarmass;
        const double jvx = 1.66007664274403694e-03 * DaysPeryear;
        const double jvy = 7.69901118419740425e-03 * DaysPeryear;
        const double jvz = -6.90460016972063023e-05 * DaysPeryear;
        const double smass = 2.85885980666130812e-04 * Solarmass;
        const double svx = -2.76742510726862411e-03 * DaysPeryear;
        const double svy = 4.99852801234917238e-03 * DaysPeryear;
        const double svz = 2.30417297573763929e-05 * DaysPeryear;
        const double umass = 4.36624404335156298e-05 * Solarmass;
        const double uvx = 2.96460137564761618e-03 * DaysPeryear;
        const double uvy = 2.37847173959480950e-03 * DaysPeryear;
        const double uvz = -2.96589568540237556e-05 * DaysPeryear;
        const double nmass = 5.15138902046611451e-05 * Solarmass;
        const double nvx = 2.68067772490389322e-03 * DaysPeryear;
        const double nvy = 1.62824170038242295e-03 * DaysPeryear;
        const double nvz = -9.51592254519715870e-05 * DaysPeryear;
        return new double [,] {
            { // sun
            Solarmass,
            0.0, 0.0, 0.0,
            (jvx * jmass + svx * smass + uvx * umass + nvx * nmass)/-Solarmass,
            (jvy * jmass + svy * smass + uvy * umass + nvy * nmass)/-Solarmass,
            (jvz * jmass + svz * smass + uvz * umass + nvz * nmass)/-Solarmass },
            { // jupiter
            jmass,
            4.84143144246472090e+00, -1.16032004402742839e+00, -1.03622044471123109e-01,
            jvx, jvy, jvz },
            { // saturn
            smass,
            8.34336671824457987e+00, 4.12479856412430479e+00, -4.03523417114321381e-01,
            svx, svy, svz },
            { // uranus
            umass,
            1.28943695621391310e+01, -1.51111514016986312e+01, -2.23307578892655734e-01,
            uvx, uvy, uvz },
            { // neptune
            nmass,
            1.53796971148509165e+01, -2.59193146099879641e+01, 1.79258772950371181e-01,
            nvx, nvy, nvz }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double sqr(double x)
    {
        return x * x;
    }
    const int L = 5;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double energy(double[,] bodies)
    {
        double e = 0.0;
        for(int i=0; i<L; i++)
        {
            double imass = bodies[i,0], ix = bodies[i,1], iy = bodies[i,2], iz = bodies[i,3];
            e += 0.5 * imass * (sqr(bodies[i,4]) + sqr(bodies[i,5]) + sqr(bodies[i,6]));
            for(int j=i+1; j<L; j++)
            {
                e -= imass * bodies[j,0] / Math.Sqrt(sqr(ix-bodies[j,1]) + sqr(iy-bodies[j,2]) + sqr(iz-bodies[j,3]));
            }
        }
        return e;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void advance(double[,] bodies)
    {
        for(int i=0; i<L; i++)
        {
            double imass = bodies[i,0], ix = bodies[i,1], iy = bodies[i,2], iz = bodies[i,3];
            double ivx = bodies[i,4], ivy = bodies[i,5], ivz = bodies[i,6];
            for(int j=i+1; j<L; j++)
            {
                double jmass = bodies[j,0], dx = bodies[j,1]-ix, dy = bodies[j,2]-iy, dz = bodies[j,3]-iz;
                double d2 = sqr(dx) + sqr(dy) + sqr(dz);
                double mag = dt / (d2 * Math.Sqrt(d2));
                ivx += dx * jmass * mag; ivy += dy * jmass * mag; ivz += dz * jmass * mag;
                bodies[j,4] -= dx * imass * mag; bodies[j,5] -= dy * imass * mag; bodies[j,6] -= dz * imass * mag;
            }
            bodies[i,1] = ix + ivx * dt; bodies[i,2] = iy + ivy * dt; bodies[i,3] = iz + ivz * dt;
            bodies[i,4] = ivx; bodies[i,5] = ivy; bodies[i,6] = ivz;
        }
    }

    public static void Main(String[] args)
    {
        var bodies = createBodies();
        Console.Out.WriteLineAsync(energy(bodies).ToString("F9"));
        int n = args.Length > 0 ? Int32.Parse(args[0]) : 10000;
        while(n-->0) advance(bodies);
        Console.Out.WriteLineAsync(energy(bodies).ToString("F9"));
    }
}