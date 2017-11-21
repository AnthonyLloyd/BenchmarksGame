// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// ported from C# version by Anthony Lloyd

open System
open System.Threading.Tasks

[<EntryPoint>]
let main args =
    let n = if args.Length=0 then 7 else int args.[0]
    let fact = Array.zeroCreate (n+1)
    fact.[0] <- 1
    let mutable factn = 1
    for i = 1 to fact.Length-1 do
        factn <- factn * i
        fact.[i] <- factn

    let chkSums = Array.zeroCreate Environment.ProcessorCount
    let maxFlips = Array.zeroCreate Environment.ProcessorCount

    let inline firstPermutation p pp count idx =
        for i = 0 to Array.length p-1 do Array.set p i i
        let rec loop i idx =
            if i>0 then
                let d = idx/Array.get fact i
                Array.set count i d
                let idx =
                    if d<=0 then idx //d=0
                    else
                        for j = 0 to i do Array.set pp j p.[j]
                        for j = 0 to i do Array.set p j pp.[(j+d) % (i+1)]
                        idx % fact.[i]
                loop (i-1) idx
        loop (count.Length-1) idx

    let inline nextPermutation p (count:int[]) =
        let mutable first = Array.get p 1
        p.[1] <- p.[0]
        p.[0] <- first
        let mutable i = 1
        while let c = count.[i]+1 in count.[i] <- c; c > i do
            count.[i] <- 0
            i <- i+1
            let next = p.[1]
            p.[0] <- next
            for j = 1 to i-1 do p.[j] <- p.[j+1]
            p.[i] <- first
            first <- next
        first

    let inline countFlips first p pp =
        if first=0 then 0
        elif Array.get p first=0 then 1
        else
            for i = 0 to Array.length pp-1 do pp.[i] <- p.[i]
            let rec loop flips first =
                let rec swap lo hi =
                    if lo<hi then
                        let t = pp.[lo]
                        pp.[lo] <- pp.[hi]
                        pp.[hi] <- t
                        swap (lo+1) (hi-1)
                swap 1 (first-1)
                let tp = pp.[first]
                if pp.[tp]=0 then flips
                else
                    pp.[first] <- first
                    loop (flips+1) tp
            loop 2 first

    let run n taskId taskSize =
        let p = Array.zeroCreate n
        let pp = Array.zeroCreate n
        let count = Array.zeroCreate n
        firstPermutation p pp count (taskId*taskSize)
        let rec loop i chksum maxflips =
            if i=0 then chksum, maxflips
            else
                let flips = countFlips (nextPermutation p count) p pp
                loop (i-1) (chksum + (1-(i%2)*2) * flips) (max flips maxflips)
        let flips = countFlips p.[0] p pp
        let chksum, maxflips = loop (taskSize-1) flips flips
        chkSums.[taskId] <- chksum
        maxFlips.[taskId] <- maxflips

    let taskSize = factn / Environment.ProcessorCount
    let threads = Array.zeroCreate Environment.ProcessorCount

    for i = 1 to Environment.ProcessorCount-1 do
        threads.[i] <- Task.Run(fun () -> run n i taskSize)
    run n 0 taskSize

    let rec loop i chksum maxflips =
        if i=threads.Length then chksum, maxflips
        else
            threads.[i].Wait()
            loop (i+1) (chksum+chkSums.[i]) (max maxflips maxFlips.[i])
    let chksum, maxflips = loop 1 chkSums.[0] maxFlips.[0]
    stdout.WriteLine (string chksum+"\nPfannkuchen("+string n+") = "+string maxflips)
    0