/* The Computer Language Benchmarks Game
  https://salsa.debian.org/benchmarksgame-team/benchmarksgame/

  contributed by Serge Smith
  further optimized (rewrote threading, random generation loop) by Jan de Vaan
  modified by Josh Goldfoot (fasta-repeat buffering)
*/

using System;
using System.Text;
using System.Buffers;
using System.Runtime.CompilerServices;

public/**/ class FastaNew
{
    const int Width = 60;
    const int Width1 = 61;
    const int LinesPerBlock = 2048;
    const int BlockSize = Width * LinesPerBlock;
    static readonly ArrayPool<byte> bytePool = ArrayPool<byte>.Shared;
    static readonly ArrayPool<int> intPool = ArrayPool<int>.Shared;
    static Tuple<byte[], int>[] blocks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static byte[] Bytes(int l, int[] rnds, byte[] vs, double[] ps)
    {
        var a = bytePool.Rent(Width1 * LinesPerBlock);
        for (int i = 0; i < l; i++)
        {
            var probability = 1.0 / 139968.0 * rnds[i];
            int j = 0;
            while (ps[j] < probability) j++;
            a[1 + i + i / Width] = vs[j];
        }
        intPool.Return(rnds);
        for (int i = 0; i <= (l - 1) / Width; i++)
            a[i * Width1] = (byte)'\n';
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int[] Rnds(int l, int j, ref int seed)
    {
        var a = intPool.Rent(BlockSize + 1);
        for (int i = 0; i < l; i++)
            a[i] = seed = (seed * 3877 + 29573) % 139968;
        a[l] = j;
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int WriteRandom(int n, int offset, int seed, byte[] vs, double[] ps)
    {
        var total = ps[0]; // cumulative probability
        for (int i = 1; i < ps.Length; i++) ps[i] = total += ps[i];

        for (int i = offset; i < offset + (n - 1) / BlockSize; i++)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(o =>
            {
                var rnds = (int[])o;
                blocks[rnds[BlockSize]] =
                Tuple.Create(Bytes(BlockSize, rnds, vs, ps),
                    Width1 * LinesPerBlock);
            }, Rnds(BlockSize, i, ref seed));
        }

        var remaining = (n - 1) % BlockSize + 1;
        System.Threading.ThreadPool.QueueUserWorkItem(o =>
        {
            var rnds = (int[])o;
            blocks[rnds[remaining]] =
            Tuple.Create(Bytes(remaining, rnds, vs, ps),
                remaining + (remaining - 1) / Width + 1);
        }, Rnds(remaining, offset + (n - 1) / BlockSize, ref seed));

        return seed;
    }

    public static byte[] /*void*/ Main(string[] args)
    {
        int n = args.Length == 0 ? 1000 : int.Parse(args[0]);
        var o = new System.IO.MemoryStream();//Console.OpenStandardOutput();
        blocks = new Tuple<byte[], int>[(8 * n - 2) / BlockSize + 3];

        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            var seed = WriteRandom(3 * n, 0, 42,
                new[] { (byte)'a', (byte)'c', (byte)'g', (byte)'t',
                    (byte)'B', (byte)'D', (byte)'H', (byte)'K', (byte)'M',
                    (byte)'N', (byte)'R', (byte)'S', (byte)'V', (byte)'W',
                    (byte)'Y'},
                        new[] {0.27,0.12,0.12,0.27,0.02,0.02,0.02,
                               0.02,0.02,0.02,0.02,0.02,0.02,0.02,0.02});

            WriteRandom(5 * n, (3 * n - 1) / BlockSize + 2, seed,
                new byte[] { (byte)'a', (byte)'c', (byte)'g', (byte)'t' },
                new[] { 0.3029549426680, 0.1979883004921,
                        0.1975473066391, 0.3015094502008 });

        });

        o.Write(Encoding.ASCII.GetBytes(">ONE Homo sapiens alu"), 0, 21);
        var table = Encoding.ASCII.GetBytes(
            "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG" +
            "GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA" +
            "CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT" +
            "ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA" +
            "GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG" +
            "AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC" +
            "AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA");
        var linesPerBlock = (LinesPerBlock / 287 + 1) * 287;
        var repeatedBytes = bytePool.Rent(Width1 * linesPerBlock);
        for (int i = 0; i <= linesPerBlock * Width - 1; i++)
            repeatedBytes[1 + i + i / Width] = table[i % 287];
        for (int i = 0; i <= (Width * linesPerBlock - 1) / Width; i++)
            repeatedBytes[i * Width1] = (byte)'\n';
        for (int i = 1; i <= (2 * n - 1) / (Width * linesPerBlock); i++)
            o.Write(repeatedBytes, 0, Width1 * linesPerBlock);
        var remaining = (2 * n - 1) % (Width * linesPerBlock) + 1;
        o.Write(repeatedBytes, 0, remaining + (remaining - 1) / Width + 1);
        bytePool.Return(repeatedBytes);
        o.Write(Encoding.ASCII.GetBytes("\n>TWO IUB ambiguity codes"), 0, 25);

        blocks[(3 * n - 1) / BlockSize + 1] = Tuple.Create
            (Encoding.ASCII.GetBytes("\n>THREE Homo sapiens frequency"), 30);

        for (int i = 0; i < blocks.Length; i++)
        {
            Tuple<byte[], int> t;
            while ((t = blocks[i]) == null) System.Threading.Thread.Sleep(0);
            o.Write(t.Item1, 0, t.Item2);
            if (t.Item2 == Width1 * LinesPerBlock) bytePool.Return(t.Item1);
        }

        o.WriteByte((byte)'\n');
        return o.ToArray();
    }
}