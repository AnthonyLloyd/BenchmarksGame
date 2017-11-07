// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// Modification by Don Syme & Jomo Fisher to use null as representation
// of Empty node and to use a single Next element.
// Based on F# version by Robert Pickering
// Based on ocaml version by Troestler Christophe & Isaac Gouy
// *reset*

open System
open System.Threading
open System.Threading.Tasks

type Next = { Left: Tree; Right: Tree }
and [<Struct>] Tree(next:Next) =
    member __.Check() =
        match box next with 
        | null -> 1
        | _ -> 1 + next.Left.Check() + next.Right.Check()
let inline out (s:string) = Console.Out.WriteLine s

[<EntryPoint>]
let main args =
    let minDepth = 4
    let maxDepth = if args.Length=0 then 10 else int args.[0]|> max (minDepth+2)
    let stretchDepth = maxDepth + 1

    let rec make depth =
        if depth=0 then Tree Unchecked.defaultof<_>
        else Tree {Left = make (depth-1); Right = make (depth-1)}

    let stretchCheck = Task.Run(fun () -> (make stretchDepth).Check().ToString())

    let longLivedTree = Task.Run(fun () ->
        let tree = make maxDepth
        tree, tree.Check().ToString() )
        
    let rec loopDepths d =
        if d<=maxDepth then
            let n = 1 <<< (maxDepth - d + minDepth)
            let c = ref 0
            Parallel.For(0, n, (fun () -> 0),
                (fun _ _ subtotal -> subtotal + (make d).Check()),
                (fun x -> Interlocked.Add(c, x) |> ignore)
            ) |> ignore
            string n+"\t trees of depth "+string d+"\t check: "+string !c|> out
            loopDepths (d+2)

    "stretch tree of depth "+string stretchDepth+
        "\t check: "+stretchCheck.Result |> out

    loopDepths minDepth

    "long lived tree of depth "+string maxDepth+
        "\t check: "+ (snd longLivedTree.Result) |> out
    0