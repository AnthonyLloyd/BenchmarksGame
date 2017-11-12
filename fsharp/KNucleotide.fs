// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/

open System

[<Literal>]
let BLOCK_SIZE = 8388608
let threeBlocks = ResizeArray<byte[]>()
let mutable threeStart = 0
let mutable threeEnd = 0

do
    let input = IO.File.OpenRead(@"C:\Users\Ant\Google Drive\BenchmarkGame\fasta25000000.txt")
    //let input = Console.OpenStandardInput()
    let mutable buffer = Array.zeroCreate BLOCK_SIZE
    let rec read offset count =
        let bytesRead = input.Read(buffer, offset, count)
        if bytesRead=count then offset+count
        elif bytesRead=0 then offset
        else read (offset+bytesRead) (count-bytesRead)

    let rec find (toFind:byte[]) (i:int) (matchIndex:int ref) =
        if !matchIndex=0 then
            let i = Array.IndexOf(buffer, toFind.[0], i)
            if -1=i then -1
            else
                matchIndex := 1
                find toFind (i+1) matchIndex
        else
            let bl = buffer.Length
            let fl = toFind.Length
            let rec tryMatch i =
                if i>=bl || !matchIndex>=fl then i
                else
                    if buffer.[i]=toFind.[!matchIndex] then
                        incr matchIndex
                        tryMatch (i+1)
                    else
                        matchIndex := 0
                        find toFind i matchIndex
            let i = tryMatch i
            if !matchIndex=fl then i else -1

    let matchIndex = ref 0
    let toFind = ">THREE"B
    let rec findStart() =
        threeEnd <- read 0 BLOCK_SIZE
        let i = find toFind 0 matchIndex
        if -1<>i then i
        else findStart()
    threeStart <- findStart()

    matchIndex := 0
    let toFind = "\n"B
    let rec findEndOfLine i =
        if i <> -1 then i
        else
            threeEnd <- read 0 BLOCK_SIZE
            find toFind 0 matchIndex |> findEndOfLine
    threeStart <- find toFind threeStart matchIndex |> findEndOfLine

    threeBlocks.Add buffer

    if threeEnd<>BLOCK_SIZE then // Needs to be at least 2 blocks
        let bytes = threeBlocks.[0]
        for i = threeEnd to bytes.Length-1 do
            bytes.[i] <- 255uy
        threeEnd <- 0
        threeBlocks.Add(Array.Empty<_>())
    else
        // find next seq or end of input
        matchIndex := 0
        let toFind = ">"B
        let rec findEnd i =
            if i <> -1 then i
            else
                buffer <- Array.zeroCreate BLOCK_SIZE
                let bytesRead = read 0 BLOCK_SIZE
                threeBlocks.Add buffer
                if bytesRead<>BLOCK_SIZE then bytesRead
                else find toFind 0 matchIndex |> findEnd
        threeEnd <- find toFind threeStart matchIndex |> findEnd

        if threeStart+18>BLOCK_SIZE then // Key needs to be in the first block
            let block0 = threeBlocks.[0]
            let block1 = threeBlocks.[1]
            Buffer.BlockCopy(block0, threeStart, block0, threeStart-18, BLOCK_SIZE-threeStart)
            Buffer.BlockCopy(block1, 0, block0, BLOCK_SIZE-18, 18)
            for i = 0 to 17 do block1.[i] <- 255uy