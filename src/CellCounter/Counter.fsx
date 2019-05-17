
#r @"C:\Users\Student\source\repos\CellCounterOld\packages\FSharpAux\lib\netstandard2.0\FSharpAux.dll"
#r @"C:\Users\Student\source\repos\CellCounterOld\packages\FSharp.Stats\lib\netstandard2.0\FSharp.Stats.dll"
#r @"C:\Users\Student\source\repos\CellCounterOld\packages\Microsoft.Xaml\lib\System.Xaml.dll"
#r @"C:\Users\Student\source\repos\CellCounterOld\packages\Microsoft.Xaml\lib\PresentationCore.dll"
#r @"C:\Users\Student\source\repos\CellCounterOld\packages\Microsoft.Xaml\lib\WindowsBase.dll"
#r @"C:\Users\Student\source\repos\CellCounterOld\packages\FSharp.Plotly\lib\netstandard2.0\FSharp.Plotly.dll"
#r @"C:\Users\Student\source\repos\CellCounterOld\src\CellCounter\bin\Release\netstandard2.0\CellCounter.dll"
#r @"C:\Users\Student\source\repos\CellCounterOld\packages\NETStandard.Library\build\netstandard2.0\ref\netstandard.dll"

open CounterFunctions
open FSharp.Plotly

let dimension = Filter.groupQuadratCalculator 6.45 2. 20. 2.

let all = Pipeline.processImage @"C:\Users\Student\OneDrive\MP_Biotech\VP_Timo\CellCounterPictures\CellCounter\tif\1.tif" dimension dimension 20. 0.2

(snd all) |> Chart.Show