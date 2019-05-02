
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


let wvArray = [|1.339646565; 2.132102385; 2.924558206; 3.717014026; 4.509469847; 5.301925668; 6.094381488; 6.886837309; 7.679293129; 8.471748950; 9.264204771|]
let wvArray2 = [|10.|]

type LocalMaxima =  
        {
        FrameNumber : int 
        X           : float
        Y           : float
        Intensity   : float
        } 

let createLocalMaxima framenumber x y intensity = 
        {
        FrameNumber = framenumber 
        X           = x
        Y           = y
        Intensity   = intensity
        }

let wavelet   = wvArray |> Array.map MarrWavelet.marrWaveletCreator

let image = Image.loadTiff @"C:\Users\Student\OneDrive\MP_Biotech\VP_Timo\CellCounterPictures\1_1_b.tif"
let paddedImage = Image.paddTiff image |> Array2D.map float
//let res = Maxima.C3DWT wavelet.[0] paddedImage

let maxima = 
    let length = wavelet.Length
    wavelet |> Array.mapi (fun i x -> 
    printfn "progress %i of %i" (i+1) length
    Maxima.C3DWT x paddedImage)

let threshold (transf: float[][]) (percentileValue: float) (maximaPositive: bool) =

    if maximaPositive then 
        let percentile = transf |> Array.concat |> Array.sort
        let cutOffValue = percentile.[int (((float percentile.Length) - 1.) * percentileValue)]
        transf
        |> JaggedArray.map (fun x -> if x < cutOffValue then 0. else x)
    else
        let percentile = transf |> Array.concat |> Array.sortDescending
        let cutOffValue = percentile.[int (((float percentile.Length) - 1.) * percentileValue)]
        transf
        |> JaggedArray.map (fun x -> if x > cutOffValue then 0. else -x)

let filteredPic =
    let selectedRectangle = Filter.rectangleSelector maxima.[9] (460, 860) (1020, 300)
    let thresholdedPic = Filter.threshold selectedRectangle 0.995 false
    thresholdedPic

//let circleCutterAdaptive (wvPicture: float[,]) (horizontalLeft: float * float) (horizontalRight: float * float) (verticalTop: float * float) (verticalBottom: float * float)=
//    let centerXY         = (fst horizontalLeft + ((fst horizontalRight - fst horizontalLeft)/2.)),(snd verticalBottom + ((snd verticalTop - snd verticalBottom)/2.))
//    let radiusHorizontal = (fst horizontalRight - fst horizontalLeft)/2.
//    let radiusVertical   = (snd verticalTop - snd verticalBottom)/2.
//    let jaggedPicture    = wvPicture |> Array2D.toJaggedArray
//    let cutPicture =    jaggedPicture
//                        |> Array.mapi (fun y -> Array.mapi (fun x value->
//                            let distanceHorizontal = abs (float x - fst centerXY)
//                            let distanceVertical = abs (float y - snd centerXY)
//                            let distanceCenter = sqrt ((float x - fst centerXY)**2. + (float y - snd centerXY)**2.)
//                            let adaptedRadius = (distanceHorizontal * radiusHorizontal + distanceVertical * radiusVertical) / (distanceHorizontal + distanceVertical)
//                                //if distanceHorizontal > distanceVertical then
//                                //    let multiplier = distanceVertical / distanceHorizontal
//                                //    let radius     = multiplier * radiusHorizontal + (1. - multiplier) * radiusVertical
//                                //    radius
//                                //else
//                                //    let multiplier = distanceHorizontal / distanceVertical
//                                //    let radius     = multiplier * radiusVertical + (1. - multiplier) * radiusHorizontal
//                                //    radius
//                            if distanceCenter > adaptedRadius then 
//                                100000000.
//                            else 
//                                value))
//    cutPicture

//let putCoordinates (array: float[][]) =
//    let mutable coordArray: float[][] = [||]
//    let result =
//        array
//        |> Array.mapi (fun y ->
//            Array.mapi (fun x value->
//                if value <> 0. then
//                    coordArray <- Array.append [|[|float x; float y|]|] coordArray
//                    coordArray
//                else 
//                    coordArray
//                    ))
//    coordArray       
            
//let coords: float[][] = putCoordinates filteredPic


//filteredPic
//|> Array.concat
//|> Chart.Histogram
//|> Chart.Show
//let dbScan = 
//    coords
//    |> FSharp.Stats.ML.Unsupervised.DbScan.compute (FSharp.Stats.ML.DistanceMetrics.euclideanNaNSquared) 1 1.5

//dbScan.Clusterlist
//|> Seq.map (fun cluster ->
//    cluster 
//    |> Seq.map (fun point -> point.[0],point.[1]
//        )
//    |> Chart.Point
//    )
//|> fun x -> Chart.Combine(Seq.append x (seq [(filteredPic |> Chart.Heatmap)]))
//|> Chart.Show




Maxima.findLocalMaxima 4 (filteredPic |> JaggedArray.toArray2D)
|> Chart.Point
|> fun x -> Chart.Combine [x |> Chart.withMarkerStyle (5, "black") ;maxima.[9] |> Array2D.toJaggedArray |>JaggedArray.transpose|> Chart.Heatmap]
|> Chart.Show


filteredPic
|> Chart.Heatmap
|> Chart.withSize (1200., 900.)
|> Chart.Show


let chart = 
    maxima.[9]
    |>Array2D.toJaggedArray
    |>Chart.Heatmap
    |>(Chart.withSize (1200., 900.))
    //|> Array.head |> fun x -> x |> Chart.Show
    |>Chart.Show

let chartAll = 
    maxima
    |>Array.map Array2D.toJaggedArray
    |>Array.map Chart.Heatmap
    |>Array.map (Chart.withSize (1200., 900.))
    //|> Array.head |> fun x -> x |> Chart.Show
    |>Array.map Chart.Show
    