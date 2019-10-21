// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// contributed by Marek Safar
// *reset*
// concurrency added by Peperud
// fixed long-lived tree by Anthony Lloyd
// ported from F# version by Anthony Lloyd

using System;

public /**/ class BinaryTreesNew
{
    struct TreeNode
    {
        class Next { public TreeNode left, right; }
        readonly Next next;

        TreeNode(TreeNode left, TreeNode right) =>
            next = new Next { left = left, right = right };

        internal static TreeNode Create(int d)
        {
            return d == 1 ? new TreeNode(new TreeNode(), new TreeNode())
                          : new TreeNode(Create(d - 1), Create(d - 1));
        }

        internal int Check()
        {
            int c = 1;
            var current = next;
            while (current != null)
            {
                c += current.right.Check() + 1;
                current = current.left.next;
            }
            return c;
        }
    }

    const int MinDepth = 4;
    const int NP = 4;
    public static byte[] /*void*/ Main(string[] args)
    {
        int maxDepth = args.Length == 0 ? 10
            : Math.Max(MinDepth + 2, int.Parse(args[0]));
        var ms = new System.IO.MemoryStream();
        var sw = new System.IO.StreamWriter(ms);//Console.OpenStandardOutput();

        TreeNode longLivedTree;
        string longLivedTreeCheck = null;
        var checks = new int[(maxDepth - MinDepth) / 2 + 1];
        System.Threading.Tasks.Parallel.For(-1, checks.Length * NP, i =>
        {
            if (i == -1)
            {
                int stretchDepth = maxDepth + 1;
                sw.WriteLine("stretch tree of depth " + stretchDepth +
                    "\t check: " + TreeNode.Create(stretchDepth).Check());
                longLivedTree = TreeNode.Create(maxDepth);
                longLivedTreeCheck = "long lived tree of depth " + maxDepth +
                    "\t check: " + longLivedTree.Check();
            }
            else
            {
                i /= NP;
                int depth = i * 2 + MinDepth;
                int n = (1 << (maxDepth - depth + MinDepth)) / NP;
                var check = 0;
                for (int j = 0; j < n; j++)
                    check += TreeNode.Create(depth).Check();
                System.Threading.Interlocked.Add(ref checks[i], check);
            }
        });

        for (int i = 0; i < checks.Length; i++)
        {
            int depth = i * 2 + MinDepth;
            int n = 1 << (maxDepth - depth + MinDepth);
            sw.WriteLine(n + "\t trees of depth " + depth + "\t check: " + checks[i]);
        }

        sw.WriteLine(longLivedTreeCheck);

        sw.Flush();
        return ms.ToArray();
    }
}