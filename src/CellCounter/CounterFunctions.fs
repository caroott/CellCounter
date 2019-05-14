
namespace CounterFunctions

open FSharpAux
open FSharp.Stats
open FSharp.Collections
open System
open System.IO
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Threading
open System.Collections.Generic

module MarrWavelet =
    
    
    type MarrWavelet =  {
        Scale           : float    
        Zero            : float    
        Minimum         : float    
        Diameter        : float    
        Values          : float [,]
        PadAreaRadius   : int      
        LMdistance      : int      
        zFilterdist     : float    
                        }

    let marrWaveletCreator (radius : float) = 
        let functionMarr x (y:float) s = (1./(Math.PI*s**2.))*(1.-(x**2.+y**2.)/(2.*s**2.))*(Math.E**(-((x**2.+y**2.)/(2.*s**2.))))
        let functionValuesMarr scale list= Array.map (fun y -> (Array.map (fun x -> functionMarr x y scale) list)) list

        {
        Scale           = 0.7071 * (radius )
        Zero            = radius   
        Minimum         = radius*2.
        Diameter        = radius*2.
        Values          = Array2D.ofJaggedArray (functionValuesMarr (0.7071 * (radius)) [|-(ceil (3. * radius + 2.))..(ceil(3. * radius + 2.))|])
        PadAreaRadius   = ceil (3. * radius + 2.) |> int 
        LMdistance      = (1.2619 * (radius) + 1.3095) |> round 0 |> int 
        zFilterdist     = 3.
}

module Image =

    let loadTiff filePath=
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

    let paddTiff (data: 'a[,])=
        let padArray2DWithRandom (rnd:System.Random) (offset:int) (arr:'a[,]) =
            let rowCount = Array2D.length1 arr 
            let colCount = Array2D.length2 arr
            let rowPadding = rowCount + offset
            let colPadding = colCount + offset
            Array2D.init (rowCount + offset * 2) (colCount + offset * 2)
                (fun rowI colI -> 
                    if (rowI < offset || colI < offset) || (rowI >= rowPadding  || colI >= colPadding) then
                        arr.[rnd.Next(0,rowCount),rnd.Next(0,colCount)] 
                    else
                        arr.[rowI-offset,colI-offset]
                )
        let paddedRawData =
            let rnd = System.Random()
            data
            |> padArray2DWithRandom rnd 40
        paddedRawData

module Maxima =

    let inline C3DWT (marr: MarrWavelet.MarrWavelet) (frame:'a[,]) =
        //the length of both sides from the picture
        let resolutionPixelfst = (Array2D.length1 frame) - (40 * 2)
        let resolutionPixelsnd = (Array2D.length2 frame) - (40 * 2)
        let offset = marr.PadAreaRadius
        let paddingoffset = 40
        let (CWTArray2D0: float[,]) = Array2D.zeroCreate (Array2D.length2 frame) (Array2D.length1 frame)
        for x = paddingoffset to (paddingoffset + (resolutionPixelsnd-1)) do
            for y = paddingoffset to (paddingoffset + (resolutionPixelfst-1)) do
                CWTArray2D0.[x,y] <-
                    let rec loop acc' a b =
                        if a <= 2 * offset then
                            if b <= 2 * offset then
                                let acc = acc' + ((marr.Values).[a,b] * (frame.[(y+(a-offset)),(x+(b-offset))] |> float))
                                loop acc a (b + 1)
                            else
                                loop acc' (a + 1) 0
                        else acc'
                    loop 0. 0 0
        let deletePaddingArea =
            let arrayWithoutPaddingoffset = Array2D.zeroCreate ((Array2D.length1 CWTArray2D0)-(2*paddingoffset)) ((Array2D.length2 CWTArray2D0)-(2*paddingoffset))
            for i=paddingoffset to (Array2D.length1 CWTArray2D0)-(paddingoffset+1) do
                for j=paddingoffset to (Array2D.length2 CWTArray2D0)-(paddingoffset+1) do
                    arrayWithoutPaddingoffset.[(i-paddingoffset),(j-paddingoffset)] <- CWTArray2D0.[i,j]
            arrayWithoutPaddingoffset
        deletePaddingArea


    ///gets Marr, a framenumber, an offset and the number of pixels to look at in the surrounding, and gives [,] of localMaxima
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

module Filter =

    let circleSelector (wvPicture: float[,]) (pointAXY: float * float) (pointBXY: float * float) =
        let centerXY        = ((fst pointAXY + fst pointBXY)/2.,(snd pointAXY + snd pointBXY)/2.)
        let radius          = (sqrt((fst pointAXY - fst pointBXY)**2. + (snd pointAXY - snd pointBXY)**2.))/2.
        let jaggedPicture   = wvPicture |> Array2D.toJaggedArray
        let cutPicture =    jaggedPicture
                            |> Array.mapi (fun y -> Array.mapi (fun x value->
                                let distanceCenter = sqrt ((float x - fst centerXY)**2. + (float y - snd centerXY)**2.)
                                if distanceCenter > radius then 0.
                                else value))
        cutPicture

    let rectangleSelector (wvPicture: float[,]) (upperLeftXY: int * int) (lowerRightXY: int * int) =
        let upperY = snd upperLeftXY
        let lowerY = snd lowerRightXY
        let leftX  = fst upperLeftXY
        let rightX = fst lowerRightXY
        let jaggedPicture = wvPicture |> Array2D.toJaggedArray
        let selectPicture =
            jaggedPicture
            |> Array.mapi 
                (fun y -> Array.mapi (fun x value->
                    if      y > upperY || y < lowerY then 0.
                    elif    x > rightX || x < leftX then 0.
                    else    value))
        selectPicture
    
    let rectangleSelectorDimensions (wvPicture: int[,]) (height: int) (width: int) =
        let center = (Array2D.length2 wvPicture) / 2, (Array2D.length1 wvPicture) / 2
        let upperY = snd center + height / 2
        let lowerY = snd center - height / 2
        let leftX  = fst center - width / 2
        let rightX = fst center + width / 2
        let jaggedPicture = wvPicture |> Array2D.toJaggedArray
        let selectPicture =
            Array.mapi 
                (fun y array -> Array.foldi (fun x acc value->
                    if      y > upperY || y < lowerY then acc
                    elif    x > rightX || x < leftX then acc
                    else    Array.append acc [|value|]) [||] array) jaggedPicture
            |> Array.filter (fun x -> not (Array.isEmpty x))
        JaggedArray.toArray2D selectPicture

    let thresholdPercentile (transf: float[][]) (percentileValue: float) (maximaPositive: bool) =

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
    
    let thresholdMaxima (image: float[,]) multiplier (maximaPositive: bool) =

        let jaggedImage         = image |> Array2D.toJaggedArray

        if maximaPositive then
            let maxima          = jaggedImage
                                  |> Array.map Array.max
                                  |> Array.sort
            let topTenAverage   = Array.take (maxima.Length / 10) maxima
                                  |> Array.average
            jaggedImage
            |> JaggedArray.map (fun x -> if x < topTenAverage * multiplier then 0. else x)
        else 
            let minima          = jaggedImage
                                  |> Array.map Array.min
                                  |> Array.sortDescending
            let topTenAverage   = Array.take (minima.Length / 10) minima
                                  |> Array.average
            jaggedImage
            |> JaggedArray.map (fun x -> if x > topTenAverage * multiplier then 0. else -x)