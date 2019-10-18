// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/ 
//
// contributed by Marek Safar
// *reset*
// concurrency added by Peperud
// fixed long-lived tree by Anthony Lloyd

using System;
using System.Threading;
using System.Threading.Tasks;

public /**/ class BinaryTreesNew
{
    const int MinDepth = 4;

    public static byte[] /*void*/ Main(string[] args)
    {
        int maxDepth = args.Length == 0 ? 10 
            : Math.Max(MinDepth + 2, int.Parse(args[0]));
        int stretchDepth = maxDepth + 1;

        var stretchTreeCheck = Task.Run(() =>
            "stretch tree of depth " + stretchDepth + "\t check: " +
                TreeNode.BottomUpTree(stretchDepth).ItemCheck());

        var longLivedTree = Task.Run(() =>
        {
            var tree = TreeNode.BottomUpTree(maxDepth);
            return ("long lived tree of depth "+ maxDepth +
                "\t check: " + tree.ItemCheck(), tree);
        });

        var results = new Task<string>[(maxDepth - MinDepth) / 2 + 1];

        for (int d = MinDepth; d <= maxDepth; d += 2)
        {
            int safe_d = d;
            results[(safe_d - MinDepth) / 2] = Task.Run(() =>
            {
                int n = 1 << (maxDepth - safe_d + MinDepth);
                int check = 0;
                Parallel.For(0, Environment.ProcessorCount, _ =>
                {
                    var c = 0;
                    for(int i = 0; i < n/Environment.ProcessorCount; i++)
                        c += TreeNode.BottomUpTree(safe_d).ItemCheck();
                    Interlocked.Add(ref check, c);
                });
                return $"{n}\t trees of depth {safe_d}\t check: {check}";
            });
        }

        var ms = new System.IO.MemoryStream();
        var sw = new System.IO.StreamWriter(ms);//Console.OpenStandardOutput();

        sw.WriteLine(stretchTreeCheck.Result);

        for (int i = 0; i < results.Length; i++)
        {
            sw.WriteLine(results[i].Result);
        }

        sw.WriteLine(longLivedTree.Result.Item1);

        sw.Flush();
        return ms.ToArray();
    }

    struct TreeNode
    {
        class Next { public TreeNode left, right; }
        Next next;

        internal static TreeNode BottomUpTree(int depth)
        {
            return depth == 0 ? new TreeNode()
              : new TreeNode(BottomUpTree(depth - 1), BottomUpTree(depth - 1));
        }

        TreeNode(TreeNode left, TreeNode right) =>
            next = new Next { left = left, right = right };

        internal int ItemCheck()
        {
            return next == null ? 1
                : 1 + next.left.ItemCheck() + next.right.ItemCheck();
        }
    }
}