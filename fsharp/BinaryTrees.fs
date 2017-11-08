// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Modification by Don Syme & Jomo Fisher to use null as representation
// of Empty node and to use a single Next element.
// Based on F# version by Robert Pickering
// Based on ocaml version by Troestler Christophe & Isaac Gouy
// *reset*

type Next = { Left: Tree; Right: Tree }
and [<Struct>] Tree(n:Next) =
    member __.Check() =
        match box n with 
        | null -> 1
        | _ -> 1 + n.Left.Check() + n.Right.Check()

[<EntryPoint>]
let main args =
    let minDepth = 4
    let maxDepth = if args.Length=0 then 10 else int args.[0]|> max (minDepth+2)
    let stretchDepth = maxDepth + 1

    let rec make depth =
        if depth=0 then Tree Unchecked.defaultof<_>
        else Tree {Left = make (depth-1); Right = make (depth-1)}

    let stretchCheck = System.Threading.Tasks.Task.Run(fun () ->
        let check = (make stretchDepth).Check().ToString()
        "stretch tree of depth "+string stretchDepth+"\t check: "+check )

    let longLivedTree = System.Threading.Tasks.Task.Run(fun () ->
        let tree = make maxDepth
        let check = tree.Check().ToString()
        let s = "long lived tree of depth "+string maxDepth+"\t check: "+check
        tree, s )
    
    let loopDepths = Array.init ((maxDepth-minDepth)/2+1) (fun d ->
        let d = minDepth+d*2
        let n = 1 <<< (maxDepth - d + minDepth)
        let c = ref 0
        System.Threading.Tasks.Parallel.For(0, n, (fun () -> 0),
            (fun _ _ subtotal -> subtotal + (make d).Check()),
            (fun x -> System.Threading.Interlocked.Add(c, x) |> ignore)
        ) |> ignore
        string n+"\t trees of depth "+string d+"\t check: "+string !c
    )

    stretchCheck.Result |> stdout.WriteLine

    loopDepths |> Array.iter stdout.WriteLine

    snd longLivedTree.Result |> stdout.WriteLine
    
    0