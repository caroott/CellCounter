
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\netstandard2.0\CellCounter.dll"

open CounterFunctions

let all = Pipeline.processAllImages @"C:\Users\Student\OneDrive\MP_Biotech\VP_Timo\CellCounterPictures\CellCounter\tif" 590 590 20. 2.
    