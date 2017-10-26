// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// contributed by Jimmy Tang
// multithreaded by Anthony Lloyd

open System
//open System.IO
let bufferSize = 1024 * 1024

type Message =
    | Read of byte[] * (int * AsyncReplyChannel<unit>) option
    | Found of (int * int)
    | NotFound of scanNext:int
    | Reversed of ((int*int) * (int*int))
    | Written of (int*int)

let mb = MailboxProcessor.Start (fun mb ->

    let pages = ResizeArray<byte[]> 256

    let scan (startPage,startIndex) (endPage,endExclusive) = async {
        let rec find page =
            let startPosition = if page=startPage then startIndex+1 else 0
            let endPosition = if page=endPage then endExclusive else bufferSize
            let i = Array.IndexOf(pages.[page], '>'B, startPosition, endPosition-startPosition)
            if i>=0 then Found (page,i) |> mb.Post
            elif page=endPage then NotFound (page+1) |> mb.Post
            else find (page+1)
        find startPage
    }

    let map = Array.init 256 byte // where?
    map.[int 'A'B] <- 'T'B
    map.[int 'B'B] <- 'V'B
    map.[int 'C'B] <- 'G'B
    map.[int 'D'B] <- 'H'B
    map.[int 'G'B] <- 'C'B
    map.[int 'H'B] <- 'D'B
    map.[int 'K'B] <- 'M'B
    map.[int 'M'B] <- 'K'B
    map.[int 'R'B] <- 'Y'B
    map.[int 'T'B] <- 'A'B
    map.[int 'V'B] <- 'B'B
    map.[int 'Y'B] <- 'R'B
    map.[int 'a'B] <- 'T'B
    map.[int 'b'B] <- 'V'B
    map.[int 'c'B] <- 'G'B
    map.[int 'd'B] <- 'H'B
    map.[int 'g'B] <- 'C'B
    map.[int 'h'B] <- 'D'B
    map.[int 'k'B] <- 'M'B
    map.[int 'm'B] <- 'K'B
    map.[int 'r'B] <- 'Y'B
    map.[int 't'B] <- 'A'B
    map.[int 'v'B] <- 'B'B
    map.[int 'y'B] <- 'R'B

    let reverse (startPage,startIndex) (endPage,endExclusive) = async {
        let rec skipHeader page =
            let startPosition = if page=startPage then startIndex+1 else 0
            let endPosition = if page=endPage then endExclusive else bufferSize
            let i = Array.IndexOf(pages.[page], '\n'B, startPosition, endPosition-startPosition)
            if i>=0 then page,i
            else skipHeader (page+1)
        let rec swap (i,iPage:byte[],iIndex) (j,jPage:byte[],jIndex) =
            let i,iPage,iIndex = if iIndex= bufferSize then i+1,pages.[i+1],0 else i,iPage,iIndex
            let j,jPage,jIndex = if jIndex= -1 then j-1,pages.[j-1],bufferSize-1 else j,jPage,jIndex
            if iIndex=jIndex && i=j then
                iPage.[iIndex] <- map.[int iPage.[iIndex]]
            elif (i,iIndex)<(j,jIndex) then
                let iValue = map.[int iPage.[iIndex]]
                iPage.[iIndex] <- map.[int jPage.[jIndex]]
                jPage.[jIndex] <- iValue
                swap (i,iPage,iIndex+1) (j,jPage,jIndex-1)
        let i,iIndex = skipHeader startPage            
        swap (i,pages.[i],iIndex+1) (endPage,pages.[endPage],endExclusive-1)
        Reversed ((startPage,startIndex),(endPage,endExclusive)) |> mb.Post
    }

    let stream = Console.OpenStandardOutput() // where?
    let write ((startPage,startIndex),(endPage,endExclusive)) = async {
        let rec write page =
            let startPosition = if page=startPage then startIndex else 0
            let endPosition = if page=endPage then endExclusive else bufferSize
            stream.Write(pages.[page], startPosition, endPosition-startPosition)
            write (page+1)
        write startPage
        Written (endPage,endExclusive) |> mb.Post
    }

    let rec loop (endExclusive,scanNext,lastScanFound,writeNext,writeList) = async {
        let inline currentEnd() = pages.Count-1, defaultArg (Option.map fst endExclusive) bufferSize
        let! msg = mb.Receive()
        let ret =
            match msg with
            | Read (bytes,endExclusive) ->
                pages.Add bytes
                if scanNext>=0 then scan (scanNext,0) (currentEnd()) |> Async.Start
                endExclusive, -1, lastScanFound, writeNext, writeList
            | Found scanFound ->
                scan scanFound (currentEnd()) |> Async.Start
                reverse lastScanFound scanFound |> Async.Start
                endExclusive, -1, scanFound, writeNext, writeList
            | NotFound scanNext ->
                if pages.Count>scanNext then
                    scan (scanNext,0) (currentEnd()) |> Async.Start
                    endExclusive, -1, lastScanFound, writeNext, writeList
                else endExclusive, scanNext, lastScanFound, writeNext, writeList
            | Reversed ((start,_) as section) ->
                if start=writeNext then
                    write section |> Async.Start
                    endExclusive, scanNext, lastScanFound, (-1,-1), writeList
                else
                    let writeList = section::writeList
                    endExclusive, scanNext, lastScanFound, writeNext, writeList
            | Written writtenTo ->
                match List.tryFind (fst>>(=)writtenTo) writeList with
                | Some section ->
                    write section |> Async.Start
                    let writeList = List.where (fst>>(<>)writtenTo) writeList
                    endExclusive, scanNext, lastScanFound, (-1,-1), writeList
                | None ->
                    if Option.isSome endExclusive && writtenTo=(currentEnd()) then
                        stream.Dispose()
                        (endExclusive.Value |> snd).Reply()
                    endExclusive, scanNext, lastScanFound, writtenTo, writeList
        return! loop ret }
    loop (None,0,(0,0),(0,0),[])
)

[<EntryPoint>]
let main _ =
    let stream = Console.OpenStandardInput()
    
    let rec read buffer offset count =
        let bytesRead = stream.Read(buffer, offset, count)
        if bytesRead=count then offset+count
        elif bytesRead=0 then offset
        else read buffer (offset+bytesRead) (count-bytesRead)
    
    let rec loop() =
        let buffer = Array.zeroCreate bufferSize
        let bytesRead = read buffer 0 bufferSize
        if bytesRead<>bufferSize then
            mb.PostAndAsyncReply(fun reply -> Read (buffer,Some (bytesRead,reply)))
        else
            Read (buffer,None) |> mb.Post
            loop()

    let wait = loop()
    stream.Dispose()
    Async.RunSynchronously wait
    0