// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// contributed by Jimmy Tang
// multithreaded by Anthony Lloyd
module FSharpReverseComplement

open System
let pageSize = 1024 * 1024

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
            let startPos = if page=startPage then startIndex+1 else 0
            let endPos = if page=endPage then endExclusive else pageSize
            let i = Array.IndexOf(pages.[page],'>'B, startPos, endPos-startPos)
            if i>=0 then Found (page,i) |> mb.Post
            elif page=endPage then NotFound (page+1) |> mb.Post
            else find (page+1)
        find startPage
    }

    let map = Array.init 256 byte
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
            let startPos = if page=startPage then startIndex+1 else 0
            let endPos = if page=endPage then endExclusive else pageSize
            let i = Array.IndexOf(pages.[page],'\n'B,startPos,endPos-startPos)
            if -1<>i then page,i else skipHeader (page+1)
        let rec swap i (iPage:byte[]) iIndex j (jPage:byte[]) jIndex =
            let mutable i,iPage,iIndex = i,iPage,iIndex
            let mutable j,jPage,jIndex = j,jPage,jIndex
            if i<j || (i=j && iIndex<=jIndex) then
                let iValue = iPage.[iIndex]
                let jValue = jPage.[jIndex]
                if iValue='\n'B || jValue='\n'B then
                    if iValue='\n'B then iIndex<-iIndex+1
                    if jValue='\n'B then jIndex<-jIndex-1
                else
                    iPage.[iIndex] <- map.[int jValue]
                    jPage.[jIndex] <- map.[int iValue]
                    iIndex <- iIndex+1
                    jIndex <- jIndex-1
                if pageSize=iIndex then
                    i <- i+1
                    iPage <- pages.[i]
                    iIndex <- 0
                if -1=jIndex then
                    j <- j-1
                    jPage <- pages.[j]
                    jIndex <- pageSize-1                
                swap i iPage iIndex j jPage jIndex
        let endPage2,endExclusive2 = if endExclusive=0 then endPage-1,pageSize-1 else endPage,endExclusive
        let i,iIndex = skipHeader startPage
        let i,iIndex = if iIndex+1=pageSize then i+1,iIndex+1 else i,iIndex
        swap i pages.[i] iIndex endPage2 pages.[endPage2] endExclusive2
        Reversed ((startPage,startIndex),(endPage,endExclusive)) |> mb.Post
    }

    let stream = IO.Stream.Null//Console.OpenStandardOutput()
    let write ((startPage,startIndex),(endPage,endExclusive)) = async {
        let rec write page =
            let startPos = if page=startPage then startIndex else 0
            let endPos = if page=endPage then endExclusive else pageSize
            stream.Write(pages.[page], startPos, endPos-startPos)
            if page<>endPage then write (page+1)
        write startPage
        Written (endPage,endExclusive) |> mb.Post
    }

    let rec loop (endExclusive,scanNext,lastFound,writeNext,writeList) = async {
        let inline currentEnd() =
            pages.Count-1,
            defaultArg (Option.map fst endExclusive) pageSize
        let! msg = mb.Receive()
        //eprintfn "%A" (match msg with | Read(_,b) -> Read([||],b) | o -> o)
        let ret =
            match msg with
            | Read (bytes,endExclusive) ->
                pages.Add bytes
                if scanNext>=0 then
                    scan (scanNext,0) (currentEnd()) |> Async.Start
                endExclusive, -1, lastFound, writeNext, writeList
            | Found scanFound ->
                scan scanFound (currentEnd()) |> Async.Start
                reverse lastFound scanFound |> Async.Start
                endExclusive, -1, scanFound, writeNext, writeList
            | NotFound scanNext ->
                if pages.Count>scanNext then
                    scan (scanNext,0) (currentEnd()) |> Async.Start
                    endExclusive, -1, lastFound, writeNext, writeList
                elif endExclusive.IsSome && scanNext=pages.Count then
                    reverse lastFound (currentEnd()) |> Async.Start
                    endExclusive, scanNext, lastFound, writeNext, writeList
                else endExclusive, scanNext, lastFound, writeNext, writeList
            | Reversed ((start,_) as section) ->
                if start=writeNext then
                    write section |> Async.Start
                    endExclusive, scanNext, lastFound, (-1,-1), writeList
                else
                    let writeList = section::writeList
                    endExclusive, scanNext, lastFound, writeNext, writeList
            | Written writtenTo ->
                match List.tryFind (fst>>(=)writtenTo) writeList with
                | Some section ->
                    write section |> Async.Start
                    let writeList = List.where (fst>>(<>)writtenTo) writeList
                    endExclusive, scanNext, lastFound, (-1,-1), writeList
                | None ->
                    if endExclusive.IsSome && writtenTo=currentEnd() then
                        stream.Dispose()
                        (snd endExclusive.Value).Reply()
                    endExclusive, scanNext, lastFound, writtenTo, writeList
        return! loop ret }
    loop (None,0,(0,0),(0,0),[])
)

//[<EntryPoint>]
let main _ =
    let stream = IO.File.OpenRead(@"C:\temp\input25000000.txt")//Console.OpenStandardInput()
    
    let rec read buffer offset count =
        let bytesRead = stream.Read(buffer, offset, count)
        if bytesRead=count then offset+count
        elif bytesRead=0 then offset
        else read buffer (offset+bytesRead) (count-bytesRead)
    
    let rec loop() =
        let buffer = Array.zeroCreate pageSize
        let bytesRead = read buffer 0 pageSize
        if bytesRead<>pageSize then
            mb.PostAndAsyncReply(fun reply ->
                Read (buffer,Some (bytesRead,reply)))
        else
            Read (buffer,None) |> mb.Post
            loop()

    let wait = loop()
    stream.Dispose()
    Async.RunSynchronously wait
    0