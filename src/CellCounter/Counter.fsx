
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\net47\CellCounter.dll"
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

let loadTiffTest filePath=
        let stream = File.OpenRead(filePath)
        let tiffDecoder =
            new TiffBitmapDecoder(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat ||| BitmapCreateOptions.IgnoreImageCache,
                    BitmapCacheOption.None);   
        Seq.init (tiffDecoder.Frames.Count) (fun frameIndex ->
            let cFrame = tiffDecoder.Frames.[frameIndex]
            let convertedBitmap = new FormatConvertedBitmap(cFrame, PixelFormats.Bgr101010, null, 0.) //new FormatConvertedBitmap(cFrame, PixelFormats.Gray16, null, 0.)
            let bytesPerPixel = convertedBitmap.Format.BitsPerPixel / 8
            let width  = convertedBitmap.PixelWidth
            let height = convertedBitmap.PixelHeight
            let stride = width * bytesPerPixel
            let bytes : byte[] = Array.zeroCreate (width * height * bytesPerPixel)
            convertedBitmap.CopyPixels(bytes, width * bytesPerPixel, 0)
            let pixelSize = bytesPerPixel
            Array2D.init width height (fun x y -> 
                BitConverter.ToInt32 (bytes,stride * y + x * pixelSize) //ToInt16 default
                )
        )
        |> Seq.head

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

let image = loadTiffTest @"C:\Users\Student\OneDrive\MP_Biotech\VP_Timo\CellCounterPictures\1_1_a.tif"
let paddedImage = Image.paddTiff image |> Array2D.map float

let inline C3DWT (marr: MarrWavelet.MarrWavelet) (frame:'a[,]) =   
    printfn "frame:\t%i\t%i" (Array2D.length1 frame) (Array2D.length2 frame)
    let resolutionPixelfst = (Array2D.length1 frame) - (40 * 2)
    let resolutionPixelsnd = (Array2D.length2 frame) - (40 * 2)
    let offset = marr.PadAreaRadius
    let paddingoffset = 40
    let (CWTArray2D0: float[,]) = Array2D.zeroCreate (Array2D.length2 frame) (Array2D.length1 frame)
    printfn "ctwarr:\t%i\t%i" (Array2D.length1 CWTArray2D0) (Array2D.length2 CWTArray2D0)
    for x = paddingoffset to (paddingoffset + (resolutionPixelsnd-1)) do
        printfn "x: %i" x
        for y = paddingoffset to (paddingoffset + (resolutionPixelfst-1)) do
            printfn "y: %i" y
            CWTArray2D0.[x,y] <-
                let mutable acc = 0.                                       
                for a = 0 to 2*offset do
                    if x > 1105 then printfn "checkp a:%i" a
                    for b = 0 to 2*offset do               
                        if x > 1105 then printfn "checkp 2 b:%i" b
                        acc <- acc + ((marr.Values).[a,b] * (frame.[(y+(a-offset)),(x+(b-offset))] |> float))
                acc
    let deletePaddingArea =
        let arrayWithoutPaddingoffset = Array2D.zeroCreate ((Array2D.length1 CWTArray2D0)-(2*paddingoffset)) ((Array2D.length2 CWTArray2D0)-(2*paddingoffset))
        for i=paddingoffset to (Array2D.length1 CWTArray2D0)-(paddingoffset+1) do
            for j=paddingoffset to (Array2D.length2 CWTArray2D0)-(paddingoffset+1) do
                arrayWithoutPaddingoffset.[(i-paddingoffset),(j-paddingoffset)] <- CWTArray2D0.[i,j]
        arrayWithoutPaddingoffset
    deletePaddingArea


let res = C3DWT wavelet.[0] paddedImage



1+1
let maxima = 
    let length = wavelet.Length
    wavelet |> Array.mapi (fun i x -> 
    printfn "progress %i of %i" (i+1) length
    C3DWT x paddedImage)

let circleCutter (wvPicture: float[,]) (pointA: float * float) (pointB: float * float) =
    let centerXY        = ((fst pointA + fst pointB)/2.,(snd pointA + snd pointB)/2.)
    let radius          = (sqrt((fst pointA - fst pointB)**2. + (snd pointA - snd pointB)**2.))/2.
    let jaggedPicture   = wvPicture |> Array2D.toJaggedArray
    let cutPicture =    jaggedPicture
                        |> Array.mapi (fun y -> Array.mapi (fun x value->
                            let distanceCenter = sqrt ((float x - fst centerXY)**2. + (float y - snd centerXY)**2.)
                            if distanceCenter > radius then 0.
                            else value))
    cutPicture

let circleCutterAdaptive (wvPicture: float[,]) (horizontalLeft: float * float) (horizontalRight: float * float) (verticalTop: float * float) (verticalBottom: float * float)=
    let centerXY         = (fst horizontalLeft + ((fst horizontalRight - fst horizontalLeft)/2.)),(snd verticalBottom + ((snd verticalTop - snd verticalBottom)/2.))
    let radiusHorizontal = (fst horizontalRight - fst horizontalLeft)/2.
    let radiusVertical   = (snd verticalTop - snd verticalBottom)/2.
    let jaggedPicture    = wvPicture |> Array2D.toJaggedArray
    let cutPicture =    jaggedPicture
                        |> Array.mapi (fun y -> Array.mapi (fun x value->
                            let distanceHorizontal = abs (float x - fst centerXY)
                            let distanceVertical = abs (float y - snd centerXY)
                            let distanceCenter = sqrt ((float x - fst centerXY)**2. + (float y - snd centerXY)**2.)
                            let adaptedRadius = (distanceHorizontal * radiusHorizontal + distanceVertical * radiusVertical) / (distanceHorizontal + distanceVertical)
                                //if distanceHorizontal > distanceVertical then
                                //    let multiplier = distanceVertical / distanceHorizontal
                                //    let radius     = multiplier * radiusHorizontal + (1. - multiplier) * radiusVertical
                                //    radius
                                //else
                                //    let multiplier = distanceHorizontal / distanceVertical
                                //    let radius     = multiplier * radiusVertical + (1. - multiplier) * radiusHorizontal
                                //    radius
                            if distanceCenter > adaptedRadius then 
                                100000000.
                            else 
                                value))
    cutPicture

circleCutterAdaptive maxima.[4] (18., 95.) (265., 95.) (140., 163.) (143., 27.)
|> Chart.Heatmap
|> Chart.withSize (900., 900.)
|> Chart.Show

let filter (orig: int[,]) (transf: float[,]) =
    let origJ = orig |> Array2D.toJaggedArray |> Array.map (Array.map (fun x -> x |> float)) |> Array.transpose
    let transfJ = transf |> Array2D.toJaggedArray
    Array.map2 (Array.map2 (fun o t -> if o <= t then t else 0.)) origJ transfJ

let difference (orig: int[,]) (transf: float[,]) =
    let origJ = orig |> Array2D.toJaggedArray |> Array.map (Array.map (fun x -> if x < 0 then -x  |> float else x |> float)) |> Array.transpose
    let transfJ = transf |> Array2D.toJaggedArray |> Array.map (Array.map (fun x -> if x < 0. then -x else x))
    let res = Array.map2 (Array.map2 (fun o t -> o - t)) origJ transfJ
    let max = Array.average (Array.map (fun x -> Array.max x) res)
    res
    |> Array.map (Array.map (fun x -> if x < (0.5 * max) then 0. else x))

let threshold (transf: float[][]) (percentileValue: float) =
    let percentile = transf |> Array.concat |> Array.sort
    let cutOffValue = percentile.[int (((float percentile.Length) - 1.) * percentileValue)]
    transf
    |> JaggedArray.map (fun x -> if x < cutOffValue then 0. else x)

let putCoordinates (array: float[][]) =
    let mutable coordArray: float[][] = [||]
    let result =
        array
        |> Array.mapi (fun y ->
            Array.mapi (fun x value->
                if value <> 0. then
                    coordArray <- Array.append [|[|float x; float y|]|] coordArray
                    coordArray
                else 
                    coordArray
                    ))
    coordArray       
            
let filteredPic = threshold (circleCutter maxima.[4] (250., 480.) (250., 50.)) 0.
let coords: float[][] = putCoordinates filteredPic

(filteredPic |> Array.concat |> Array.filter (fun x -> x <> 0.)).Length
(filteredPic|> Array.concat).Length

filteredPic
|> Array.concat
|> Chart.Histogram
|> Chart.Show
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



////////////////venny bgin////
let inline findLocalMaxima dist frame =   
        ///gets single 2D Array with only Maxima in it and gives coordinates of local maxima
        let allmaximaArray (arr:float[,]) =
            let rec loop acc i j =
                if i < (Array2D.length1 arr)-1 then
                    if j < (Array2D.length2 arr)-1  then 
                        if (arr.[i,j]) > 0. then loop ((float i, float j)::acc) i (j+1) 
                        else loop acc i (j+1)    
                    else loop acc (i+1) 0
                else acc
            loop [] 0 0 
        let numberofsurpix = dist
        let (cWTPercArray: float [,]) = frame  
        let arrayOfMaxima = Array2D.zeroCreate ((Array2D.length1 cWTPercArray)) ((Array2D.length2 cWTPercArray))
        let checkListsForContinuousDecline b c numberofsurpix =      
            let createSurroundingPixelLists b c numberofsurpix =
                let rec loop i accN accS accW accE accNW accSW accNE accSE =
                    let imod = (i |> float) * 0.7071 |> floor |> int
                    if i <= numberofsurpix then 
                        loop (i+1)  (cWTPercArray.[b+i   ,c     ]::accN )
                                    (cWTPercArray.[b-i   ,c     ]::accS )
                                    (cWTPercArray.[b     ,c-i   ]::accW )
                                    (cWTPercArray.[b     ,c+i   ]::accE )
                                    (cWTPercArray.[b+imod,c-imod]::accNW)
                                    (cWTPercArray.[b-imod,c-imod]::accSW)
                                    (cWTPercArray.[b+imod,c+imod]::accNE)
                                    (cWTPercArray.[b-imod,c+imod]::accSE)
                    else [accN;accS;accW;accE;accNW;accSW;accNE;accSE]
                loop 0 [] [] [] [] [] [] [] [] 
    
            let surroundingList = createSurroundingPixelLists b c numberofsurpix

            let rec isSortedAsc (list: float list) = 
                match list with
                    | [] -> true
                    | [x] -> true
                    | x::((y::_)as t) -> if x > y then false else isSortedAsc(t) 
            let boolList = surroundingList |> List.map  (fun x -> isSortedAsc x)
            (boolList |> List.contains false) = false

        //calculates checkListsForContinuousDecline for every pixel
        for i=numberofsurpix to (Array2D.length1 cWTPercArray)-(numberofsurpix+1) do 
            for j=numberofsurpix to (Array2D.length2 cWTPercArray)-(numberofsurpix+1) do 
                if cWTPercArray.[i,j] >= 10. then                              
                    if checkListsForContinuousDecline i j numberofsurpix = true     
                        then arrayOfMaxima.[i,j] <- cWTPercArray.[i,j]              
                    else arrayOfMaxima.[i,j] <- 0.                                  
                else arrayOfMaxima.[i,j] <- 0.                                   
        allmaximaArray arrayOfMaxima

findLocalMaxima 4 (filteredPic |> JaggedArray.toArray2D)
|> Chart.Point
|> fun x -> Chart.Combine [x;filteredPic |> JaggedArray.transpose|> Chart.Heatmap]
|> Chart.Show


////////////////venny end//////////

filteredPic
|> Chart.Heatmap
|> Chart.withSize (900., 900.)
|> Chart.Show

[[1.; 2.];[3.;4.]]
|> Chart.Heatmap
|> Chart.withSize (900., 900.)
|> Chart.Show

res
|> Array2D.toJaggedArray
|> Array.transpose
|> Chart.Heatmap
//|> Chart.withSize (900., 900.)
|> Chart.Show

threshold maxima.[4]
|> Chart.Heatmap
|> Chart.withSize (900., 900.)
|> Chart.Show


let chart = 
    maxima.[4]
    |>Array2D.toJaggedArray
    |>Chart.Heatmap
    |>(Chart.withSize (900., 900.))
    //|> Array.head |> fun x -> x |> Chart.Show
    |>Chart.Show

    