// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// ported from C# version by Anthony Lloyd

open System
open System.Collections.Generic

type Counter =
  {
    mutable X0 : int16; mutable X1 : int16; mutable X2 : int16; mutable X3 : int16
    mutable X4 : int16; mutable X5 : int16; mutable X6 : int16; mutable X7 : int16
    mutable X8 : int16; mutable X9 : int16; mutable X10 : int16; mutable X11 : int16
    mutable X12 : int16; mutable X13 : int16; mutable X14 : int16; mutable X15 : int16
  }
  static member Create i =
    let c = {X0=0s;X1=0s;X2=0s;X3=0s;X4=0s;X5=0s;X6=0s;X7=0s;X8=0s;X9=0s;X10=0s;X11=0s;X12=0s;X13=0s;X14=0s;X15=0s}
    c.Inc i
    c
  member c.Inc i = match i with |0->c.X0<-c.X0+1s|1->c.X1<-c.X1+1s|2->c.X2<-c.X2+1s|3->c.X3<-c.X3+1s
                                |4->c.X4<-c.X4+1s|5->c.X5<-c.X5+1s|6->c.X6<-c.X6+1s|7->c.X7<-c.X7+1s
                                |8->c.X8<-c.X8+1s|9->c.X9<-c.X9+1s|10->c.X10<-c.X10+1s|11->c.X11<-c.X11+1s
                                |12->c.X12<-c.X12+1s|13->c.X13<-c.X13+1s|14->c.X14<-c.X14+1s|15->c.X15<-c.X15+1s
                                |_ ->()
  member c.Get i = match i with |0->c.X0|1->c.X1|2->c.X2|3->c.X3
                                |4->c.X4|5->c.X5|6->c.X6|7->c.X7
                                |8->c.X8|9->c.X9|10->c.X10|11->c.X11
                                |12->c.X12|13->c.X13|14->c.X14|15->c.X15
                                |_ ->0s


[<Literal>]
let BLOCK_SIZE = 8388608 // 1024 * 1024 * 8

[<EntryPoint>]
let main _ =
  let threeStart,threeBlocks,threeEnd =
    use input = IO.File.OpenRead(@"C:\temp\fasta25000000.txt")
    let mutable threeEnd = 0
    let read buffer =
        let rec read offset count =
            let bytesRead = input.Read(buffer, offset, count)
            if bytesRead=count then offset+count
            elif bytesRead=0 then offset
            else read (offset+bytesRead) (count-bytesRead)
        threeEnd <- read 0 BLOCK_SIZE

    let rec findHeader matchIndex buffer =
        let toFind = ">THREE"B
        let find i matchIndex =
            let rec find i matchIndex =
                if matchIndex=0 then
                    let i = Array.IndexOf(buffer, toFind.[0], i)
                    if -1=i then -1,0
                    else find (i+1) 1
                else
                    let fl = toFind.Length
                    let rec tryMatch i matchIndex =
                        if i>=BLOCK_SIZE || matchIndex>=fl then i,matchIndex
                        else
                            if buffer.[i]=toFind.[matchIndex] then
                                tryMatch (i+1) (matchIndex+1)
                            else
                                find i 0
                    let i,matchIndex = tryMatch i matchIndex
                    if matchIndex=fl then i,matchIndex else -1,matchIndex
            find i matchIndex
        read buffer
        let i,matchIndex = find 0 matchIndex
        if -1<>i then i,buffer
        else findHeader matchIndex buffer

    let rec findSequence i buffer =
        let i = Array.IndexOf(buffer, '\n'B, i)
        if i <> -1 then buffer,i+1
        else
            read buffer
            findSequence 0 buffer

    let buffer,threeStart = Array.zeroCreate BLOCK_SIZE
                            |> findHeader 0 ||> findSequence

    let threeBlocks =
        if threeEnd<>BLOCK_SIZE then // Needs to be at least 2 blocks
            for i = threeEnd to BLOCK_SIZE-1 do
                buffer.[i] <- 255uy
            threeEnd <- 0
            [[||];buffer]
        else
            let rec findEnd i buffer threeBlocks =
                let i = Array.IndexOf(buffer, '>'B, i)
                if i <> -1 then
                    threeEnd <- i
                    buffer::threeBlocks
                else
                    let threeBlocks = buffer::threeBlocks
                    let buffer = Array.zeroCreate BLOCK_SIZE
                    read buffer
                    if threeEnd<>BLOCK_SIZE then buffer::threeBlocks
                    else findEnd 0 buffer threeBlocks
            let threeBlocks = findEnd threeStart buffer []
            if threeStart+18>BLOCK_SIZE then // Key needs to be in first block
                let block0 = threeBlocks.[0]
                let block1 = threeBlocks.[1]
                Buffer.BlockCopy(block0, threeStart, block0, threeStart-18,
                    BLOCK_SIZE-threeStart)
                Buffer.BlockCopy(block1, 0, block0, BLOCK_SIZE-18, 18)
                for i = 0 to 17 do block1.[i] <- 255uy
            threeBlocks

    threeStart, List.rev threeBlocks |> List.toArray, threeEnd

  let toChar = [|'A'; 'C'; 'G'; 'T'|]
  let toNum = Array.zeroCreate 256
  toNum.[int 'c'B] <- 1uy; toNum.[int 'C'B] <- 1uy
  toNum.[int 'g'B] <- 2uy; toNum.[int 'G'B] <- 2uy
  toNum.[int 't'B] <- 3uy; toNum.[int 'T'B] <- 3uy
  toNum.[int '\n'B] <- 255uy; toNum.[int '>'B] <- 255uy; toNum.[255] <- 255uy

  Array.Parallel.iter (fun bs ->
    for i = 0 to Array.length bs-1 do
        bs.[i] <- toNum.[int bs.[i]]
  ) threeBlocks

  let count l mask (summary:_->string) = async {
      let mutable rollingKey = 0
      let firstBlock = threeBlocks.[0]
      let rec startKey l start =
          if l>0 then
             rollingKey <- rollingKey <<< 2 ||| int firstBlock.[start]
             startKey (l-1) (start+1)
      startKey l threeStart
      let dict = Dictionary()
      let inline check a lo hi =
        for i = lo to hi do
          let nb = Array.get a i
          if nb<>255uy then
              rollingKey <- rollingKey &&& mask <<< 2 ||| int nb
              match dict.TryGetValue rollingKey with
              | true, v -> incr v
              | false, _ -> dict.[rollingKey] <- ref 1

      check firstBlock (threeStart+l) (BLOCK_SIZE-1)
      
      for i = 1 to threeBlocks.Length-2 do
          check threeBlocks.[i] 0 (BLOCK_SIZE-1)
          
      let lastBlock = threeBlocks.[threeBlocks.Length-1]
      check lastBlock 0 (threeEnd-1)
      return summary dict
    }

  let writeFrequencies fragmentLength (freq:Dictionary<_,_>) =
    let percent = 100.0 / (Seq.sumBy (!) freq.Values |> float)
    freq |> Seq.sortByDescending (fun kv -> kv.Value)
    |> Seq.collect (fun kv ->
        let keyChars = Array.zeroCreate fragmentLength
        let mutable key = kv.Key
        for i in keyChars.Length-1..-1..0 do
            keyChars.[i] <- toChar.[int key &&& 0x3]
            key <- key >>> 2
        [String(keyChars);" ";(float !kv.Value * percent).ToString("F3");"\n"]
      )
    |> String.Concat

  let writeCount (fragment:string) (dict:Dictionary<_,_>) =
    let mutable key = 0
    for i = 0 to fragment.Length-1 do
        key <- key <<< 2 ||| int toNum.[int fragment.[i]]
    let b,v = dict.TryGetValue key
    String.Concat((if b then string !v else "0"), "\t", fragment)

  let count64 l mask (summary:_->string) = async {
      let mutable rollingKey = 0L
      let firstBlock = threeBlocks.[0]
      let rec startKey l start =
            if l>0 then
               rollingKey <- rollingKey <<< 2 ||| int64 firstBlock.[start]
               startKey (l-1) (start+1)
      startKey l threeStart
      let dict = Dictionary()
      let inline check a lo hi =
          for i = lo to hi do
            let nb = Array.get a i
            if nb<>255uy then
              rollingKey <- rollingKey &&& mask <<< 2 ||| int64 nb
              let k = uint32(rollingKey>>>4)
              let i = int rollingKey &&& 15
              match dict.TryGetValue k with
              | true, a -> (a:Counter).Inc i
              | false, _ -> dict.[k] <- Counter.Create i
            
      check firstBlock (threeStart+l) (BLOCK_SIZE-1)

      for i = 1 to threeBlocks.Length-2 do
          check threeBlocks.[i] 0 (BLOCK_SIZE-1)

      let lastBlock = threeBlocks.[threeBlocks.Length-1]
      check lastBlock 0 (threeEnd-1)

      return summary dict
   }

  let writeCount64 (fragment:string) (dict:Dictionary<_,Counter>) =
    let mutable key = 0L
    for i = 0 to fragment.Length-3 do
        key <- key <<< 2 ||| int64 toNum.[int fragment.[i]]
    let i = int64 toNum.[int fragment.[fragment.Length-2]] <<< 2
        ||| int64 toNum.[int fragment.[fragment.Length-1]]
    let b,v = dict.TryGetValue(uint32 key)
    String.Concat((if b then string(v.Get(int i)) else "0"), "\t", fragment)

  let results =
    Async.Parallel [
      count64 18 0x7FFFFFFFFL (writeCount64 "GGTATTTTAATTTATAGT")
      count 12 0x7FFFFF (writeCount "GGTATTTTAATT")
      count 6 0x3FF (writeCount "GGTATT")
      count 4 0x3F (writeCount "GGTA")
      count 3 0xF (writeCount "GGT")
      count 2 0x3 (writeFrequencies 2)
      count 1 0 (writeFrequencies 1)
    ]
    |> Async.RunSynchronously
  
  stdout.WriteLine results.[6]
  stdout.WriteLine results.[5]
  stdout.WriteLine results.[4]
  stdout.WriteLine results.[3]
  stdout.WriteLine results.[2]
  stdout.WriteLine results.[1]
  stdout.WriteLine results.[0]

  0