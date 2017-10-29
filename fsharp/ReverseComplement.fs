// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/
//
// contributed by Jimmy Tang
// multithreaded by Anthony Lloyd
module FSharpReverseComplement

open System
open System.Threading.Tasks
let pageSize = 1024 * 1024
let pages = Array.zeroCreate 256
let scans = Array.zeroCreate<Task<int>> 256
type Message =
    | Read of ((int * int) * AsyncReplyChannel<unit>) option
    | Found of (int * int)
    | NotFound of scanNext:int
    | Reversed of ((int*int) * (int*int))
    | Written of (int*int)

let mb = MailboxProcessor.Start (fun mb ->

    let map = Array.init 256 byte
    Array.iter2 (fun i v -> map.[int i] <- v)
        "ABCDGHKMRTVYabcdghkmrtvy"B
        "TVGHCDMKYABRTVGHCDMKYABR"B
    
    let scan (startPage,startIndex) = async {
        let rec find page =
            let pageBytes = pages.[page]
            if isNull pageBytes then NotFound page |> mb.Post
            else
                let startPos = if page=startPage then startIndex+1 else 0
                let i = if startPos=0 then scans.[page].Result
                        else Array.IndexOf(pageBytes,'>'B, startPos)
                if i>=0 then Found (page,i) |> mb.Post
                else find (page+1)
        find startPage
    }

    let reverse (startPage,startIndex) (endPage,endExclusive) = async {
        let rec skipHeader page =
            let startPos = if page=startPage then startIndex+1 else 0
            let endPos = if page=endPage then endExclusive else pageSize
            let i = Array.IndexOf(pages.[page],'\n'B,startPos,endPos-startPos)
            if -1<>i then page,i+1 else skipHeader (page+1)
        do        
            let mutable i,iIndex = skipHeader startPage
            let mutable j,jIndex = endPage,endExclusive-1
            let mutable iPage,jPage = pages.[i],pages.[j]
            let inline adjustij() =
                if pageSize=iIndex then
                    i <- i+1
                    iPage <- pages.[i]
                    iIndex <- 0        
                if -1=jIndex then
                    j <- j-1
                    jPage <- pages.[j]
                    jIndex <- pageSize-1
            while (adjustij(); i<j || (i=j && iIndex<=jIndex)) do
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
        let! msg = mb.Receive()
        //match msg with | Read(_) -> () | o -> eprintfn "%A" o
        let ret =
            match msg with
            | Read endExclusive ->
                if scanNext<> -1 then scan (scanNext,0) |> Async.Start
                endExclusive, -1, lastFound, writeNext, writeList
            | Found scanFound ->
                scan scanFound |> Async.Start
                reverse lastFound scanFound |> Async.Start
                endExclusive, -1, scanFound, writeNext, writeList
            | NotFound scanNext ->
                match endExclusive with
                | Some ((page,_),_) when page+1 = scanNext ->
                    reverse lastFound (fst endExclusive.Value) |> Async.Start
                    endExclusive, scanNext, lastFound, writeNext, writeList
                | _ -> 
                    scan (scanNext,0) |> Async.Start
                    endExclusive, -1, lastFound, writeNext, writeList
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
                    if endExclusive.IsSome && writtenTo=(fst endExclusive.Value) then
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
    
    let rec loop i =
        let buffer = Array.zeroCreate pageSize
        let bytesRead = read buffer 0 pageSize
        if i<>0 then
            scans.[i] <- Task.Run(fun () -> Array.IndexOf(buffer,'>'B))
        pages.[i] <- buffer
        if bytesRead<>pageSize then
            mb.PostAndAsyncReply(fun reply -> (Some ((i,bytesRead), reply)) |> Read)
        else
            if i=0 then Read None |> mb.Post
            loop (i+1)

    let wait = loop 0
    stream.Dispose()
    Async.RunSynchronously wait
    0