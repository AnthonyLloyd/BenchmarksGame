/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
    
   started with Java #2 program (Krause/Whipkey/Bennet/AhnTran/Enotus/Stalcup)
   adapted for C# by Jan de Vaan
   simplified and optimised to use TPL by Anthony Lloyd
*/

using System;
using System.Threading.Tasks;

public class MandelBrot
{
    public static byte[] Main (String[] args)
    {
        var n = args.Length > 0 ? Int32.Parse(args[0]) : 200;
        //Console.Out.WriteLineAsync("P4\n" + n + " " + n);
        double invN=2.0/n;
        int lineLen = (n-1)/8 + 1;
        var data = new byte[n*lineLen];
        Parallel.For(0, n, y =>
        {
            var Ciby = y*invN-1.0;
            //var offset = y*lineLen;
            for(int x=0; x<lineLen; x++)
            {
                var Crbx = x*8*invN-1.5;
                int res=0;
                for(int i=0;i<4;i++)
                {
                    double Crbx1=Crbx+invN;
                    double Zr1=Crbx, Zr2=Crbx1;
                    double Zi1=Ciby, Zi2=Ciby;

                    int b=0;
                    int j=49;
                    do
                    {
                        double nZr1=Zr1*Zr1-Zi1*Zi1+Crbx;
                        Zi1=Zr1*Zi1+Zr1*Zi1+Ciby;
                        Zr1=nZr1;

                        double nZr2=Zr2*Zr2-Zi2*Zi2+Crbx1;
                        Zi2=Zr2*Zi2+Zr2*Zi2+Ciby;
                        Zr2=nZr2;

                        if(Zr1*Zr1+Zi1*Zi1>4){b|=2;if(b==3)break;}
                        if(Zr2*Zr2+Zi2*Zi2>4){b|=1;if(b==3)break;}
                    } while(--j>0);
                    res=(res<<2)+b;
                    Crbx = Crbx1+invN;
                }
                data[y*lineLen+x] = (byte)(res^-1);
            }
        });
        
        //Console.OpenStandardOutput().Write(data, 0, data.Length);
        return data;
    }
}