
using System;
using System.Numerics;
using System.Text;

public class pidigits
{
    BigInteger q = new BigInteger(), r = new BigInteger(), s = new BigInteger(), t = new BigInteger();
    BigInteger u = new BigInteger(), v = new BigInteger(), w = new BigInteger();

    int i;
    StringBuilder strBuf = new StringBuilder(40), lastBuf = null;
    int n;

    public pidigits(int n)
    {
        this.n = n;
    }

    private void compose_r(int bq, int br, int bs, int bt)
    {
        u = r * bs;
        r *= bq;
        v = t * br;
        r += v;
        t *= bt;
        t += u;
        s *= bt;
        u = q * bs;
        s += u;
        q *= bq;
    }

    /* Compose matrix with numbers on the left. */
    private void compose_l(int bq, int br, int bs, int bt)
    {
        r *= bt;
        u = q * br;
        r += u;
        u = t * bs;
        t *= bt;
        v = s * br;
        t += v;
        s *= bq;
        s += u;
        q *= bq;
    }

    /* Extract one digit. */
    private int extract(int j)
    {
        u = q * j;
        u += r;
        v = s * j;
        v += t;
        w = u / v;
        return (int)w;
    }

    /* Print one digit. Returns 1 for the last digit. */
    private bool prdigit(int y, bool verbose)
    {
        strBuf.Append(y);
        if (++i % 10 == 0 || i == n)
        {
            if (i % 10 != 0)
                for (int j = 10 - (i % 10); j > 0; j--)
                { strBuf.Append(" "); }
            strBuf.Append("\t:");
            strBuf.Append(i);
            if (verbose) Console.WriteLine(strBuf);
            lastBuf = strBuf;
            strBuf = new StringBuilder(40);
        }
        return i == n;
    }

    /* Generate successive digits of PI. */
    public void Run(bool verbose)
    {
        int k = 1;
        i = 0;
        q = 1;
        r = 0;
        s = 0;
        t = 1;
        for (; ; )
        {
            int y = extract(3);
            if (y == extract(4))
            {
                if (prdigit(y, verbose))
                    return;
                compose_r(10, -10 * y, 0, 1);
            }
            else
            {
                compose_l(k, 4 * k + 2, 0, 2 * k + 1);
                k++;
            }
        }
    }

    public static void Main(String[] args)
    {
        int n = (args.Length > 0 ? Int32.Parse(args[0]) : 10);
        new pidigits(n).Run(false);
    }
}
