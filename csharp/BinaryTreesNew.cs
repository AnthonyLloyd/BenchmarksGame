// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// contributed by Marek Safar
// concurrency added by Peperud
// fixed long-lived tree by Anthony Lloyd
// ported from F# version by Anthony Lloyd

using System;
using System.Threading.Tasks;

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
    const int NoTasks = 4;
    public static byte[] /*void*/ Main(string[] args)
    {
        int maxDepth = args.Length == 0 ? 10
            : Math.Max(MinDepth + 2, int.Parse(args[0]));

        var stretchTreeCheck = Task.Run(() =>
        {
            int stretchDepth = maxDepth + 1;
            return "stretch tree of depth " + stretchDepth + "\t check: " +
                   TreeNode.Create(stretchDepth).Check();
        });

        var longLivedTree = Task.Run(() =>
        {
            var tree = TreeNode.Create(maxDepth);
            return ("long lived tree of depth " + maxDepth +
                "\t check: " + tree.Check(), tree);
        });

        var results = new string[(maxDepth - MinDepth) / 2 + 1];

        for (int i = 0; i < results.Length; i++)
        {
            int depth = i * 2 + MinDepth;
            int n = (1 << maxDepth - depth + MinDepth) / NoTasks;
            var tasks = new Task<int>[NoTasks];
            for (int t = 0; t < tasks.Length; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    var check = 0;
                    for (int i = n; i > 0; i--)
                        check += TreeNode.Create(depth).Check();
                    return check;
                });
            }
            var check = tasks[0].Result;
            for (int t = 1; t < tasks.Length; t++)
                check += tasks[t].Result;
            results[i] = (n * NoTasks) + "\t trees of depth " + depth +
                         "\t check: " + check;
        }

        var ms = new System.IO.MemoryStream();
        var sw = new System.IO.StreamWriter(ms);//Console.OpenStandardOutput();

        sw.WriteLine(stretchTreeCheck.Result);

        for (int i = 0; i < results.Length; i++)
        {
            sw.WriteLine(results[i]);
        }

        sw.WriteLine(longLivedTree.Result.Item1);

        sw.Flush();
        return ms.ToArray();
    }
}