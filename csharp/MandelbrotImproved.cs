/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   started with Java #2 program (Krause/Whipkey/Bennet/AhnTran/Enotus/Stalcup)
   adapted for C# by Jan de Vaan
   simplified and optimised to use TPL by Anthony Lloyd
   simplified to compute Cib alongside Crb by Tanner Gooding
   optimized to use Vector<double> by Tanner Gooding
*/

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class MandelBrot
{
    public static unsafe byte[] Main(string[] args)
    {
        var size = args.Length==0 ? 200 : int.Parse(args[0]);
        // Console.Out.WriteAsync(String.Concat("P4\n",size," ",size,"\n"));
        var lineLength = size >> 3;
        var data = new byte[size * lineLength];
        fixed (byte* pdata = &data[0])
        {
            var _pdata = pdata;
            Parallel.For(0, size, y =>
            {
                var invN = new Vector<double>(2.0/size);
                var onePtFive = new Vector<double>(1.5);
                var step = invN * 2;
                var stepi = new Vector<double>(8);
                var Ciby = invN*y-Vector<double>.One;
                var Crbxi = new Vector<double>(new double[] {0,1,0,0,0,0,0,0});
                for (var x=0; x<lineLength; x++)
                {
                    var Crbx = Crbxi * invN - onePtFive;
                    var res = 0;
                    for (var i=0; i<8; i+=2)
                    {
                        var Zr = Crbx;
                        var Zi = Ciby;
                        int b = 0, j = 49;
                        do
                        {
                            var nZr = Zr * Zr - Zi * Zi + Crbx;
                            var ZrZi = Zr * Zi; 
                            Zi = ZrZi + ZrZi + Ciby;
                            Zr = nZr;
                            var t = Zr * Zr + Zi * Zi;
                            if (t[0]>4.0) { b|=2; if (b==3) break; }
                            if (t[1]>4.0) { b|=1; if (b==3) break; }
                        } while (--j>0);
                        res = (res << 2) + b;
                        Crbx += step;
                    }
                    _pdata[y*lineLength+x] = (byte)(res^-1);
                    Crbxi += stepi;
                }
            });
            // Console.OpenStandardOutput().Write(data, 0, data.Length);
            return data;
        }
    }
}