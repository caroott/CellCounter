
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\netstandard2.0\CellCounter.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\FSharpAux.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\FSharp.Stats.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\FSharp.Plotly.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\System.Xaml.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\PresentationCore.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\WindowsBase.dll"
#r @"C:\Users\Student\source\repos\BioFSharp\bin\BioFSharp.ImgP\net47\BioFSharp.ImgP.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\netstandard2.0\CellCounter.dll"

open CounterFunctions

let all = Pipeline.processAllImages @"C:\Users\Student\OneDrive\MP_Biotech\VP_Timo\CellCounterPictures\CellCounter\tif" 590 590 20. 2.
    