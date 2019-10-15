open System
open Expecto

let private allDiffs (s1:byte array) (s2:byte array) =
  Seq.mapi2 (fun i s p -> i,s,p) s1 s2
  |> Seq.take (min s1.Length s2.Length)
  |> Seq.fold (fun (onFrom,l) v ->
      match onFrom, v with
      | None,(i,f,s) when f<>s -> Some i, l
      | Some i,(j,f,s) when f=s -> None, (i,j)::l
      | onFrom,_ -> onFrom, l
  ) (None,[])
  |> snd
  |> List.last

/// Expects function `f1` is faster than `f2`. Measurer used to measure only a
/// subset of the functions. Statistical test to 99.99% confidence level.
let isFasterThanSub (f1:Performance.Measurer<_,_>->'a) (f2:Performance.Measurer<_,_>->'a) format =
  let toString (s:SampleStatistics) =
    sprintf "%.4f \u00B1 %.4f ms" s.mean s.meanStandardError

  match Performance.timeCompare f1 f2 with
  | Performance.ResultNotTheSame (r1, r2)->
    printfn "%s. Expected function results to be the same (%A vs %A)." format r1 r2
    match box r1, box r2 with
    | (:? (byte[]) as r1), (:? (byte[]) as r2) ->
        printfn "r1 length: %i" r1.Length
        printfn "r2 length: %i" r2.Length
        printfn "all diffs:\n%A" (allDiffs r1 r2)
    | _ -> ()
  | Performance.MetricTooShort (s,p) ->
    printfn "%s. Expected metric (%s) to be much longer than the machine resolution (%s)." format (toString s) (toString p)
  | Performance.MetricEqual (s1,s2) ->
    printfn "%s. Expected f1 (%s) to be faster than f2 (%s) but are equal." format (toString s1) (toString s2)
  | Performance.MetricMoreThan (s1,s2) ->
    printfn "%s. Expected f1 (%s) to be faster than f2 (%s) but is ~%.0f%% slower." format (toString s1) (toString s2) ((s1.mean/s2.mean-1.0)*100.0)
  | Performance.MetricLessThan (s1,s2) ->
    printfn "%s. f1 (%s) is %s faster than f2 (%s)." format (toString s1) (sprintf "~%.1f%%" ((1.0-s1.mean/s2.mean)*100.0)) (toString s2)

/// Expects function `f1` is faster than `f2`. Statistical test to 99.99%
/// confidence level.
let isFasterThan (f1:unit->'a) (f2:unit->'a) message =
  isFasterThanSub (fun measurer -> measurer f1 ())
                  (fun measurer -> measurer f2 ())
                  message


[<EntryPoint>]
let main argv =
    //let n = 300
    //printfn "Simple"
    //PerfSimple.causalProfiling n (fun () -> FastaNew.Main([|"25000000"|]) |> ignore)
    //printfn "Faithful"
    //Perf.causalProfiling n (fun () -> FastaNew.Main([|"25000000"|]) |> ignore)
    isFasterThan (fun () -> FastaNew.Main([|"25000000"|]))
                 (fun () -> FastaOld.Main([|"25000000"|])) ""
    0