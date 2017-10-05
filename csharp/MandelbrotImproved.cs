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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe byte GetByte(double* pCrb, double Ciby)
    {
        var res = 0;
        for (var i=0; i<8; i+=2)
        {
            var vCrbx = Unsafe.Read<Vector<double>>(pCrb+i);
            var vCiby = new Vector<double>(Ciby);
            var Zr = vCrbx;
            var Zi = vCiby;
            int b = 0, j = 49;
            do
            {
                var nZr = Zr * Zr - Zi * Zi + vCrbx;
                var ZrZi = Zr * Zi; 
                Zi = ZrZi + ZrZi + vCiby;
                Zr = nZr;
                var t = Zr * Zr + Zi * Zi;
                if (t[0]>4.0) { b|=2; if (b==3) break; }
                if (t[1]>4.0) { b|=1; if (b==3) break; }
            } while (--j>0);
            res = (res << 2) + b;
        }
        return (byte)(res^-1);
    }
    public static unsafe byte[] Main(string[] args)
    {
        var size = args.Length==0 ? 200 : int.Parse(args[0]);
        // Console.Out.WriteAsync(String.Concat("P4\n",size," ",size,"\n"));
        var adjustedSize = (size + (Vector<double>.Count * 8))
                         & ~(Vector<double>.Count * 8);

        var Crb = new double[adjustedSize];
        var Cib = new double[adjustedSize];

        var lineLength = size >> 3;
        var data = new byte[adjustedSize * lineLength];
        fixed (double* pCrb = &Crb[0])
        fixed (double* pCib = &Cib[0])
        fixed (byte* pdata = &data[0])
        {
            var value = new Vector<double>(
                  Vector<double>.Count==2 ? new double[] {0,1}
                : Vector<double>.Count==4 ? new double[] {0,1,2,3}
                : new double[] {0,1,2,3,4,5,6,7}
            );
            var invN = new Vector<double>(2.0/size);
            var onePtFive = new Vector<double>(1.5);
            var one = Vector<double>.One;
            var step = new Vector<double>(Vector<double>.Count);
            for (var i=0; i<size; i+=Vector<double>.Count)
            {
                var t = value * invN;
                Unsafe.Write(pCrb+i, t-onePtFive);
                Unsafe.Write(pCib+i, t-one);
                value += step;
            }
            var _Crb = pCrb;
            var _Cib = pCib;
            var _pdata = pdata;
            Parallel.For(0, size, y =>
            {
                var Ciby = _Cib[y];
                for (var x=0; x<lineLength; x++)
                {
                    _pdata[y*lineLength+x] = GetByte(_Crb+x*8, Ciby);
                }
            });
            // Console.OpenStandardOutput().Write(data, 0, data.Length);
            return data;
        }
    }
}