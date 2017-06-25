/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, optimization and use of more C# idioms by Robert F. Tobler
*/

namespace Improved
{
    using System;
    using System.Threading;

    class Body { public double x, y, z, vx, vy, vz, mass; }
    struct Pair { public Body bi, bj; }

    public class NBody
    {
        static Body[] createBodies()
        {
            const double Pi = 3.141592653589793;
            const double Solarmass = 4 * Pi * Pi;
            const double DaysPeryear = 365.24;
            var jupiter = new Body {
                    x = 4.84143144246472090e+00,
                    y = -1.16032004402742839e+00,
                    z = -1.03622044471123109e-01,
                    vx = 1.66007664274403694e-03 * DaysPeryear,
                    vy = 7.69901118419740425e-03 * DaysPeryear,
                    vz = -6.90460016972063023e-05 * DaysPeryear,
                    mass = 9.54791938424326609e-04 * Solarmass,
                };
            var saturn = new Body {
                    x = 8.34336671824457987e+00,
                    y = 4.12479856412430479e+00,
                    z = -4.03523417114321381e-01,
                    vx = -2.76742510726862411e-03 * DaysPeryear,
                    vy = 4.99852801234917238e-03 * DaysPeryear,
                    vz = 2.30417297573763929e-05 * DaysPeryear,
                    mass = 2.85885980666130812e-04 * Solarmass,
                };
            var uranus = new Body {
                    x = 1.28943695621391310e+01,
                    y = -1.51111514016986312e+01,
                    z = -2.23307578892655734e-01,
                    vx = 2.96460137564761618e-03 * DaysPeryear,
                    vy = 2.37847173959480950e-03 * DaysPeryear,
                    vz = -2.96589568540237556e-05 * DaysPeryear,
                    mass = 4.36624404335156298e-05 * Solarmass,
                };
            var neptune = new Body {
                    x = 1.53796971148509165e+01,
                    y = -2.59193146099879641e+01,
                    z = 1.79258772950371181e-01,
                    vx = 2.68067772490389322e-03 * DaysPeryear,
                    vy = 1.62824170038242295e-03 * DaysPeryear,
                    vz = -9.51592254519715870e-05 * DaysPeryear,
                    mass = 5.15138902046611451e-05 * Solarmass,
                };
            var sun = new Body {
                    mass = Solarmass,
                    vx = (jupiter.vx * jupiter.mass + saturn.vx * saturn.mass
                         +uranus.vx * uranus.mass + neptune.vx * neptune.mass)/-Solarmass,
                    vy = (jupiter.vy * jupiter.mass + saturn.vy * saturn.mass
                         +uranus.vy * uranus.mass + neptune.vy * neptune.mass)/-Solarmass,
                    vz = (jupiter.vz * jupiter.mass + saturn.vz * saturn.mass
                         +uranus.vz * uranus.mass + neptune.vz * neptune.mass)/-Solarmass,
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

        static int largeLoopLength;
        static volatile int largeLoopId = 0;
        static volatile int secondLoopId = 0;
        
        static void advance(Body[] bodies, Pair[] pairs, int step, int istep)
        {
            const double dt = 0.01;
            var mod = pairs.Length;
            var modStep = mod/step;
            while(true)
            {
                while(largeLoopId%modStep!=istep) {}
                int i = largeLoopId%mod;
                if(i!=mod-1) largeLoopId++;
                if(largeLoopId>=largeLoopLength) break;
                var p = pairs[i];
                Body bi = p.bi, bj = p.bj;
                double dx = bi.x - bj.x, dy = bi.y - bj.y, dz = bi.z - bj.z;
                double d2 = dx * dx + dy * dy + dz * dz;
                double mag = dt / (d2 * Math.Sqrt(d2));
                while(secondLoopId%mod!=i) {}
                bi.vx -= dx * bj.mass * mag; bj.vx += dx * bi.mass * mag;
                bi.vy -= dy * bj.mass * mag; bj.vy += dy * bi.mass * mag;
                bi.vz -= dz * bj.mass * mag; bj.vz += dz * bi.mass * mag;
                secondLoopId++;
                if(i==mod-1)
                {
                    foreach (var b in bodies)
                    {
                        b.x += dt * b.vx; b.y += dt * b.vy; b.z += dt * b.vz;
                    }
                    largeLoopId++;
                }
            }
        }

        static double energy(Body[] bodies)
        {
            double e = 0.0;
            for (int i = 0; i < bodies.Length; i++)
            {
                var bi = bodies[i];
                e += 0.5 * bi.mass * (bi.vx*bi.vx + bi.vy*bi.vy + bi.vz*bi.vz);
                for (int j = i+1; j < bodies.Length; j++)
                {
                    var bj = bodies[j];
                    double dx = bi.x - bj.x, dy = bi.y - bj.y, dz = bi.z - bj.z;
                    e -= (bi.mass * bj.mass) / Math.Sqrt(dx*dx + dy*dy + dz*dz);
                }
            }
            return e;
        }

        public static void Main(String[] args)
        {
            
            var bodies = createBodies();
            Console.WriteLine(energy(bodies).ToString("f9"));
            var pairs = createPairs(bodies);
            largeLoopLength =
                (args.Length > 0 ? Int32.Parse(args[0]) : 10000)
                * pairs.Length;
            new Thread(() => advance(bodies, pairs, 2, 0)).Start();
            new Thread(() => advance(bodies, pairs, 2, 1)).Start();
            new Thread(() => advance(bodies, pairs, 2, 2)).Start();
            new Thread(() => advance(bodies, pairs, 2, 3)).Start();
            advance(bodies, pairs, 2, 4);
            Console.WriteLine(energy(bodies).ToString("f9"));
        }

        public static Tuple<double,double> Test(String[] args)
        {            
            var bodies = createBodies();
            var startEnergy = energy(bodies);
            var pairs = createPairs(bodies);
            largeLoopId = secondLoopId = 0;
            largeLoopLength =
                (args.Length > 0 ? Int32.Parse(args[0]) : 10000)
                * pairs.Length;
            new Thread(() => advance(bodies, pairs, 2, 0)).Start();
            new Thread(() => advance(bodies, pairs, 2, 1)).Start();
            new Thread(() => advance(bodies, pairs, 2, 2)).Start();
            new Thread(() => advance(bodies, pairs, 2, 3)).Start();
            advance(bodies, pairs, 2, 4);
            var endEnergy = energy(bodies);
            return Tuple.Create(Math.Round(startEnergy,10), Math.Round(endEnergy,10));
        }
    }
}