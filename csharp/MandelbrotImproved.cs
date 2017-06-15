/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
    
   started with Java #2 program (Krause/Whipkey/Bennet/AhnTran/Enotus/Stalcup)
   adapted for C# by Jan de Vaan
*/

namespace Improved
{

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;

public class MandelBrot
{
    // private static double[] Crb;
    // private static double[] Cib;

     [MethodImpl(MethodImplOptions.AggressiveInlining)]
     static byte getByte(double[] Crb, double Ciby, int x, int y)
     {
        int res=0;
        for(int i=0;i<8;i+=2)
        {
            double Zr1=Crb[x+i], Zr2=Crb[x+i+1];
            double Zi1=Ciby, Zi2=Ciby;

            int b=0;
            int j=49;
            do
            {
                double nZr1=Zr1*Zr1-Zi1*Zi1+Crb[x+i];
                Zi1=Zr1*Zi1+Zr1*Zi1+Ciby;
                Zr1=nZr1;

                double nZr2=Zr2*Zr2-Zi2*Zi2+Crb[x+i+1];
                Zi2=Zr2*Zi2+Zr2*Zi2+Ciby;
                Zr2=nZr2;

                if(Zr1*Zr1+Zi1*Zi1>4){b|=2;if(b==3)break;}
                if(Zr2*Zr2+Zi2*Zi2>4){b|=1;if(b==3)break;}
            } while(--j>0);
            res=(res<<2)+b;
        }
        return (byte)(res^-1);
    }
    
    public static byte[] Test (String[] args)
    {
        var n = args.Length > 0 ? Int32.Parse(args[0]) : 200;
        var Crb = new double[n+7];
        double invN=2.0/n; for(int i=0;i<n;i++){ Crb[i]=i*invN-1.5; }
        var data = new byte[n][];
        int lineLen = (n-1)/8 + 1;
        Parallel.For(0, n, y =>
        {
            var Ciby = y*invN-1.0;
            var buffer = new byte[lineLen];
            for(int x = 0; x<lineLen; x++)
                 buffer[x] = getByte(Crb, Ciby, x*8, y);
            data[y] = buffer;
        });
        var s = new MemoryStream();
        for (int y = 0; y < data.Length; y++) s.Write(data[y], 0, lineLen);
        return s.ToArray();
    }
}

}