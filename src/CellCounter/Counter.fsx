
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\netstandard2.0\CellCounter.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\FSharpAux.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\FSharp.Stats.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\FSharp.Plotly.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\System.Xaml.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\PresentationCore.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\WindowsBase.dll"
#r @"C:\Users\Student\source\repos\BioFSharp\bin\BioFSharp.ImgP\net47\BioFSharp.ImgP.dll"

open CounterFunctions
open FSharp.Collections
open FSharpAux
open FSharp.Stats
open FSharp.Plotly
open System.Windows.Media
open System.Windows.Media.Imaging
open System.IO
open System
open BioFSharp.ImgP
open BioFSharp.ImgP

let thresholdFilter (image: float[,]) (maximaPositive: bool) =
    let jaggedImage         = image |> Array2D.toJaggedArray

    if maximaPositive then
        let maxima          = jaggedImage
                              |> Array.map Array.max
                              |> Array.sort
        let topTenAverage   = Array.take (maxima.Length / 10) maxima
                              |> Array.average
        jaggedImage
        |> JaggedArray.map (fun x -> if x < topTenAverage*2. then 0. else x)
    else 
        let minima          = jaggedImage
                              |> Array.map Array.min
                              |> Array.sortDescending
        let topTenAverage   = Array.take (minima.Length / 10) minima
                              |> Array.average
        jaggedImage
        |> JaggedArray.map (fun x -> if x > topTenAverage*2. then 0. else -x)

let readAllImages folderPath height width radius =
    let imagePaths        = Directory.GetFiles folderPath
    let images            = imagePaths
                            |> Array.map Image.loadTiff
    let selectedImages    = images
                            |> Array.map (fun x -> Filter.rectangleSelectorDimensions x height width)
    let paddedImages      = Centroid.paddTiff selectedImages
                            |> Array.map (Array2D.map float)
    let transformedImages = paddedImages 
                            |> Array.map (fun x -> Maxima.C3DWT (MarrWavelet.marrWaveletCreator radius) x)

    let thresholdedImages = transformedImages
                            |> Array.map (fun x -> thresholdFilter x  false)
    //let thresholdedImages = transformedImages
    //                        |> Array.map (fun x -> Filter.threshold (x |> Array2D.toJaggedArray) 0.995 false)
    thresholdedImages

let all = readAllImages @"C:\Users\Student\OneDrive\MP_Biotech\VP_Timo\CellCounterPictures" 590 590 8.


all
//|> Array.map Array2D.toJaggedArray
|>Array.map Chart.Heatmap
//|>Array.map (Chart.withSize (1200., 900.))
//|> Array.head |> fun x -> x |> Chart.Show
|>Array.map Chart.Show




//Maxima.findLocalMaxima 4 (filteredPic |> JaggedArray.toArray2D)
//|> Chart.Point
//|> fun x -> Chart.Combine [x |> Chart.withMarkerStyle (5, "black") ;maxima.[9] |> Array2D.toJaggedArray |>JaggedArray.transpose|> Chart.Heatmap]
//|> Chart.Show


//filteredPic
//|> Chart.Heatmap
//|> Chart.withSize (1200., 900.)
//|> Chart.Show


//let chart = 
//    maxima.[9]
//    |>Array2D.toJaggedArray
//    |>Chart.Heatmap
//    |>(Chart.withSize (1200., 900.))
//    //|> Array.head |> fun x -> x |> Chart.Show
//    |>Chart.Show

//let chartAll = 
//    maxima
//    |>Array.map Array2D.toJaggedArray
//    |>Array.map Chart.Heatmap
//    |>Array.map (Chart.withSize (1200., 900.))
//    //|> Array.head |> fun x -> x |> Chart.Show
//    |>Array.map Chart.Show
    