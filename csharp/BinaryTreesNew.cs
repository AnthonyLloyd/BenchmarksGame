// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// contributed by Marek Safar
// concurrency added by Peperud
// fixed long-lived tree by Anthony Lloyd
// ported from F# version by Anthony Lloyd

using System;

public /**/ class BinaryTreesNew
{
    struct TreeNode
    {
        class Next { public TreeNode left, right; }
        readonly Next n;

        TreeNode(TreeNode left, TreeNode right) =>
            n = new Next { left = left, right = right };

        internal static TreeNode Create(int d)
        {
            return d == 1 ? new TreeNode(new TreeNode(), new TreeNode())
                          : new TreeNode(Create(d - 1), Create(d - 1));
        }

        internal int Check()
        {
            int c = 1;
            var current = n;
            while (current != null)
            {
                c += current.right.Check() + 1;
                current = current.left.n;
            }
            return c;
        }
    }

    const int MinDepth = 4;

    public static byte[] /*void*/ Main(string[] args)
    {
        int maxDepth = args.Length == 0 ? 10
                : Math.Max(MinDepth + 2, int.Parse(args[0]));
        var ms = new System.IO.MemoryStream();
        var sw = new System.IO.StreamWriter(ms);//Console.OpenStandardOutput();

        TreeNode longLivedTree;
        string longLivedTreeCheck = null;
        var checks = new int[(maxDepth - MinDepth)/2 + 1];
        System.Threading.Tasks.Parallel.For(-1, checks.Length, i =>
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
                int depth = i * 2 + MinDepth;
                var check = 0;
                for (int j = 1 << maxDepth - depth + MinDepth; j-- > 0;)
                    check += TreeNode.Create(depth).Check();
                checks[i] = check;
            }
        });

        for (int i = 0; i < checks.Length; i++)
        {
            int depth = i * 2 + MinDepth;
            sw.WriteLine((1 << maxDepth - depth + MinDepth) +
                "\t trees of depth " + depth + "\t check: " + checks[i]);
        }

        sw.WriteLine(longLivedTreeCheck);

        sw.Flush();
        return ms.ToArray();
    }
}