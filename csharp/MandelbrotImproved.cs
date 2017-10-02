/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
    
   started with Java #2 program (Krause/Whipkey/Bennet/AhnTran/Enotus/Stalcup)
   adapted for C# by Jan de Vaan
   simplified and optimised to use TPL by Anthony Lloyd
*/

using System;
using System.Numerics;
using System.Threading.Tasks;

public static class MandelBrot
{
    public static byte[] Main (String[] args)
    {
        var n = args.Length==0 ? 200 : Int32.Parse(args[0]);
        //Console.Out.WriteLineAsync("P4\n" + n + " " + n);
        var invN = new Vector<double>(2.0/n);
        var constY = new Vector<double>(-1.0);
        var constX = new Vector<double>(new double[]{-1.5, 2.0/n-1.5, 0.0, 0.0}); 
        int lineLen = (n-1)/8 + 1;
        var data = new byte[n*lineLen];
        Parallel.For(0, n, y =>
        {
            var Ciby = invN*y + constY;
            for(int x=0; x<lineLen; x++)
            {
                var Crbx = invN*(x*8) + constX;
                int res = 0;
                for(int i=0; i<4; i++)
                {
                    Vector<double> Zi=Ciby, Zr=Crbx;
                    int b=0, j=49;
                    do
                    {
                        var nZr=Zr*Zr-Zi*Zi+Crbx;
                        Zi=Zr*Zi*2.0+Ciby;
                        Zr=nZr;
                        var t=Zr*Zr+Zi*Zi;
                        if(t[0]>4.0){b|=2;if(b==3)break;}
                        if(t[1]>4.0){b|=1;if(b==3)break;}
                    } while(--j>0);
                    res=(res<<2)+b;
                    Crbx += invN*2.0;
                }
                data[y*lineLen+x] = (byte)~res;
            }
        });
        //Console.OpenStandardOutput().Write(data, 0, data.Length);
        return data;
    }
}