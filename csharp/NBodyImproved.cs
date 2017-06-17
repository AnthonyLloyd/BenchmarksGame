/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, optimization and use of more C# idioms by Robert F. Tobler
*/

namespace Improved
{
    using System;

    public class NBody {
        public static void Main(String[] args) {
            int n = args.Length > 0 ? Int32.Parse(args[0]) : 10000;
            var bodies = new NBodySystem();
            Console.WriteLine(bodies.Energy().ToString("f9"));
            bodies.Advance(0.01, n);
            Console.WriteLine(bodies.Energy().ToString("f9"));
        }
        public static Tuple<double,double> Test(int n)
        {            
            var bodies = new NBodySystem();
            var startEnergy = bodies.Energy();
            bodies.Advance(0.01, n);
            var endEnergy = bodies.Energy();
            return Tuple.Create(Math.Round(startEnergy,10), Math.Round(endEnergy,10));
        }
    }

    class Body { public double x, y, z, vx, vy, vz, mass; }

    class NBodySystem
    {
        const double Pi = 3.141592653589793;
        const double Solarmass = 4 * Pi * Pi;
        const double DaysPeryear = 365.24;
        Body[] bodies;

        public NBodySystem()
        {
            var sun = new Body {mass = Solarmass};
            bodies = new Body[]
            {
                sun,
                new Body { // Jupiter
                    x = 4.84143144246472090e+00,
                    y = -1.16032004402742839e+00,
                    z = -1.03622044471123109e-01,
                    vx = 1.66007664274403694e-03 * DaysPeryear,
                    vy = 7.69901118419740425e-03 * DaysPeryear,
                    vz = -6.90460016972063023e-05 * DaysPeryear,
                    mass = 9.54791938424326609e-04 * Solarmass,
                },
                new Body { // Saturn
                    x = 8.34336671824457987e+00,
                    y = 4.12479856412430479e+00,
                    z = -4.03523417114321381e-01,
                    vx = -2.76742510726862411e-03 * DaysPeryear,
                    vy = 4.99852801234917238e-03 * DaysPeryear,
                    vz = 2.30417297573763929e-05 * DaysPeryear,
                    mass = 2.85885980666130812e-04 * Solarmass,
                },
                new Body { // Uranus
                    x = 1.28943695621391310e+01,
                    y = -1.51111514016986312e+01,
                    z = -2.23307578892655734e-01,
                    vx = 2.96460137564761618e-03 * DaysPeryear,
                    vy = 2.37847173959480950e-03 * DaysPeryear,
                    vz = -2.96589568540237556e-05 * DaysPeryear,
                    mass = 4.36624404335156298e-05 * Solarmass,
                },
                new Body { // Neptune
                    x = 1.53796971148509165e+01,
                    y = -2.59193146099879641e+01,
                    z = 1.79258772950371181e-01,
                    vx = 2.68067772490389322e-03 * DaysPeryear,
                    vy = 1.62824170038242295e-03 * DaysPeryear,
                    vz = -9.51592254519715870e-05 * DaysPeryear,
                    mass = 5.15138902046611451e-05 * Solarmass,
                }
            };            
            double px = 0.0, py = 0.0, pz = 0.0;
            for(int i=0; i<bodies.Length; i++)
            {
                var bi = bodies[i];    
                px += bi.vx * bi.mass; py += bi.vy * bi.mass; pz += bi.vz * bi.mass;
            }
            sun.vx = -px/Solarmass; sun.vy = -py/Solarmass; sun.vz = -pz/Solarmass;
        }

        public void Advance(double dt, int n)
        {
            for (; n>0; n--)
            {
                for (int i=0; i<bodies.Length; i++)
                {
                    var bi = bodies[i];
                    for (int j=i+1; j<bodies.Length; j++)
                    {
                        var bj = bodies[j];
                        double dx = bi.x - bj.x, dy = bi.y - bj.y, dz = bi.z - bj.z;
                        double d2 = dx * dx + dy * dy + dz * dz;
                        double mag = dt / (d2 * Math.Sqrt(d2));
                        bi.vx -= dx * bj.mass * mag; bj.vx += dx * bi.mass * mag;
                        bi.vy -= dy * bj.mass * mag; bj.vy += dy * bi.mass * mag;
                        bi.vz -= dz * bj.mass * mag; bj.vz += dz * bi.mass * mag;
                    }
                }
                for (int i=0; i<bodies.Length; i++)
                {
                    var bi = bodies[i];
                    bi.x += dt * bi.vx; bi.y += dt * bi.vy; bi.z += dt * bi.vz;
                }
            }
        }

        public double Energy()
        {
            double e = 0.0;
            for (int i=0; i<bodies.Length; i++)
            {
                var bi = bodies[i];
                e += 0.5 * bi.mass * (bi.vx*bi.vx + bi.vy*bi.vy + bi.vz*bi.vz);
                for (int j=i+1; j<bodies.Length; j++)
                {
                    var bj = bodies[j];
                    double dx = bi.x - bj.x, dy = bi.y - bj.y, dz = bi.z - bj.z;
                    e -= (bi.mass * bj.mass) / Math.Sqrt(dx*dx + dy*dy + dz*dz);
                }
            }
            return e;
        }
    }
}