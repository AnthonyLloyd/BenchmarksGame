open System
open Expecto
open System.Security.Cryptography

/// Expects function `f1` is faster than `f2`. Measurer used to measure only a
/// subset of the functions. Statistical test to 99.99% confidence level.
let isFasterThanSub (f1:Performance.Measurer<_,_>->'a) (f2:Performance.Measurer<_,_>->'a) format =
  let toString (s:SampleStatistics) =
    sprintf "%.4f \u00B1 %.4f ms" s.mean s.meanStandardError

  match Performance.timeCompare f1 f2 with
  | Performance.ResultNotTheSame (r1, r2)->
    printfn "%s. Expected function results to be the same (%A vs %A)." format r1 r2
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
    let start = System.Diagnostics.Stopwatch.GetTimestamp()
    
    
    let fsData = Mandelbrot.main [|"16000"|]
    
    
    
    let end1 = System.Diagnostics.Stopwatch.GetTimestamp()
    Console.WriteLine(float(end1-start)*1000.0/float System.Diagnostics.Stopwatch.Frequency)

    let csData = MandelBrot.Main [|"16000"|]

    
    Seq.mapi2 (fun i f c -> i,f,c) fsData csData
    |> Seq.where (fun (_,f,c) -> f<>c)
    |> Seq.truncate 30
    |> Seq.iter (fun (i,f,c) -> printfn "%i\t%A\t%A" i f c)



    // let start = System.Diagnostics.Stopwatch.GetTimestamp()
    // //Mandelbrot.main [||] |> ignore
    // //Mandelbrot.main [|"16000"|] |> ignore
    // MandelBrot.Main [|"16000"|] |> ignore
    // //KNucleotide.main [||] |> ignore
    // //KNucleotideCS.Main [||] |> ignore
    // let end1 = System.Diagnostics.Stopwatch.GetTimestamp()
    // Console.WriteLine(float(end1-start)*1000.0/float System.Diagnostics.Stopwatch.Frequency)
    0
    //Improved.MandelBrot.Main([|"16000"|])
    //isFasterThan (fun () -> MandelBrot.Main [|"16000"|]) (fun () -> MandelBrotOld.Main [|"16000"|]) "Improved C# Mandelbrot faster than original"

    // NBody.Main([|"50000000"|])
    // Improved.NBody.Main([|"5"|])
    // NBody.Test [|"50000000"|] |> ignore

    // KNucleotideCS.LoadThreeData()
    // if KNucleotideCS.threeStart<>KNucleotide.threeStart then printfn "Start: %i %i" KNucleotideCS.threeStart KNucleotide.threeStart
    // if KNucleotideCS.threeEnd<>KNucleotide.threeEnd then printfn "End: %i %i" KNucleotideCS.threeEnd KNucleotide.threeEnd
    // Seq.iteri2 (fun i a1 a2 ->
    //   if a1<>a2 then printfn "diff %i" i
    // ) KNucleotideCS.threeBlocks KNucleotide.threeBlocks
    

    // Create big faster file
    // Fasta.Main(argv)
    // revcompImproved.Main argv
    // isFasterThanSub (fun m -> m (fun () -> revcompImproved.Main argv) (); revcompImproved.Reset())
    //                 (fun m -> m (fun () -> revcomp.Main argv) (); revcomp.Reset()) "Improved C# ReverseComplement faster than original"

    // let start = System.Diagnostics.Stopwatch.GetTimestamp();
    // revcompImproved.Main argv
    // let end1 = System.Diagnostics.Stopwatch.GetTimestamp();
    // Console.WriteLine(float(end1-start)*1000.0/float System.Diagnostics.Stopwatch.Frequency);

    // let start = System.Diagnostics.Stopwatch.GetTimestamp();
    // revcomp.Main argv
    // let end1 = System.Diagnostics.Stopwatch.GetTimestamp();
    // revcompImproved.Main argv
    // let end2 = System.Diagnostics.Stopwatch.GetTimestamp();
    // Console.WriteLine(float(end1-start)*1000.0/float System.Diagnostics.Stopwatch.Frequency);
    // Console.WriteLine(float(end2-end1)*1000.0/float System.Diagnostics.Stopwatch.Frequency);

    // let start = System.Diagnostics.Stopwatch.GetTimestamp()
    // FannkuchRedux.Main [|"12"|]// |> printfn "%A"
    // let end1 = System.Diagnostics.Stopwatch.GetTimestamp()
    // Console.WriteLine(float(end1-start)*1000.0/float System.Diagnostics.Stopwatch.Frequency)    
    // let start = System.Diagnostics.Stopwatch.GetTimestamp()
    // KNucleotideOld.Main [||]// |> printfn "%A"
    // let end1 = System.Diagnostics.Stopwatch.GetTimestamp()
    // KNucleotide.Main [||]// |> printfn "%A"
    // let end2 = System.Diagnostics.Stopwatch.GetTimestamp()
    // Console.WriteLine(float(end1-start)*1000.0/float System.Diagnostics.Stopwatch.Frequency)
    // Console.WriteLine(float(end2-end1)*1000.0/float System.Diagnostics.Stopwatch.Frequency)
    // Console.WriteLine((12.37/float(end1-start)*float(end2-end1)).ToString("F2")+" compared to 7.93")

    // isFasterThan (fun () -> NBody.Main [|"50000000"|]) (fun () -> NBodyOld.Main [|"50000000"|]) "Improved C# n-body faster than original"

    //isFasterThan (fun () -> KNucleotideImproved.Main argv) (fun () -> KNucleotide.Main argv) "Improved C# KNucleotide faster than original"

    //isFasterThan (fun () -> FSharpImprovedNBody.test 5000000) (fun () -> FSharpOriginalNBody.test 5000000) "NBody F# Improved faster then F# Original"
    //isFasterThan (fun () -> CSharpParallel.NBody.Test 5000000) (fun () -> CSharpOriginal.NBody.Test 5000000) "NBody C# Parallel faster then C# Original"


    //KNucleotideImproved.Main argv

    // let start = System.Diagnostics.Stopwatch.GetTimestamp();
    // KNucleotide.Main argv
    // let end1 = System.Diagnostics.Stopwatch.GetTimestamp();
    // Console.WriteLine(float(end1-start)*1000.0/float System.Diagnostics.Stopwatch.Frequency);

    // 0 // return an integer exit code

