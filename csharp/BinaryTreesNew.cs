// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// contributed by Marek Safar
// *reset*
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

        internal int ItemCheck()
        {
            int c = 1;
            var current = next;
            while (current != null)
            {
                c += current.right.ItemCheck() + 1;
                current = current.left.next;
            }
            return c;
        }
    }

    const int MinDepth = 4;

    public static byte[] /*void*/ Main(string[] args)
    {
        int maxDepth = args.Length == 0 ? 10
            : Math.Max(MinDepth + 2, int.Parse(args[0]));
        int stretchDepth = maxDepth + 1;

        var stretchTreeCheck = Task.Run(() =>
            "stretch tree of depth " + stretchDepth + "\t check: " +
                TreeNode.Create(stretchDepth).ItemCheck());

        var longLivedTree = Task.Run(() =>
        {
            var tree = TreeNode.Create(maxDepth);
            return ("long lived tree of depth " + maxDepth +
                "\t check: " + tree.ItemCheck(), tree);
        });

        var results = new Task<string>[(maxDepth - MinDepth) / 2 + 1];

        for (int i = 0; i < results.Length; i++)
        {
            int depth = i * 2 + MinDepth;
            results[i] = Task.Run(() =>
            {
                int pc = Environment.ProcessorCount * 2;
                int n = (1 << (maxDepth - depth + MinDepth)) / pc;
                var tasks = new Task<int>[pc - 1];
                for (int i = 0; i < tasks.Length; i++)
                    tasks[i] = Task.Run(() =>
                    {
                        var check = 0;
                        for (int i = 0; i < n; i++)
                            check += TreeNode.Create(depth).ItemCheck();
                        return check;
                    });

                int check = 0;
                for (int i = 0; i < n; i++)
                    check += TreeNode.Create(depth).ItemCheck();

                var s = (n * pc) + "\t trees of depth " + depth + "\t check: ";

                for (int i = 0; i < tasks.Length; i++)
                    check += tasks[i].Result;

                return s + check;
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
}