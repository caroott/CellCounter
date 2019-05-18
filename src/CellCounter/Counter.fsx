
#r @"C:\Users\Student\source\repos\CellCounter\packages\FSharpAux\lib\netstandard2.0\FSharpAux.dll"
#r @"C:\Users\Student\source\repos\CellCounter\packages\FSharp.Stats\lib\netstandard2.0\FSharp.Stats.dll"
#r @"C:\Users\Student\source\repos\CellCounter\packages\Microsoft.Xaml\lib\System.Xaml.dll"
#r @"C:\Users\Student\source\repos\CellCounter\packages\Microsoft.Xaml\lib\PresentationCore.dll"
#r @"C:\Users\Student\source\repos\CellCounter\packages\Microsoft.Xaml\lib\WindowsBase.dll"
#r @"C:\Users\Student\source\repos\CellCounter\packages\FSharp.Plotly\lib\netstandard2.0\FSharp.Plotly.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\netstandard2.0\CellCounter.dll"
#r @"C:\Users\Student\source\repos\CellCounter\packages\NETStandard.Library\build\netstandard2.0\ref\netstandard.dll"

open System
open System.IO
open CounterFunctions
open FSharp.Plotly

let dimension = Filter.groupQuadratCalculator 6.45 2. 20. 2.


let processFolders folderPath height width radius multiplier = 
    let subfolders  = Directory.GetDirectories folderPath
    let result      = subfolders
                      |> Array.mapi (fun i folder ->
                            printfn "evaluating folder %i of %i" (i+1) subfolders.Length
                            Pipeline.processImages folder height width radius multiplier) 
    result

let growthCurve = processFolders @"C:\Users\Student\OneDrive\MP_Biotech\VP_Timo\GrowthCurve\NoGrid" dimension dimension 20. 0.2

growthCurve.[12]
|> Array.map (fun (x,y) -> y |> Chart.Show)
