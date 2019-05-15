
namespace CounterFunctions

open FSharp.Collections
open FSharpAux
open FSharp.Stats
open FSharp.Plotly
open System.Windows.Media
open System.Windows.Media.Imaging
open System.IO
open System

module MarrWavelet =
    
    //The functions in this module are taken from BioFSharp.ImgP
    
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

    //The functions in this module are taken from BioFSharp.ImgP and modified for use in this project

    ///This function takes a string. It returns an int 2DArray.
    ///The returned 2DArray is a representation of the pixels in the file given by the filepath.

    let loadTiff filePath=
        let stream = File.OpenRead(filePath)
        let tiffDecoder =
            new TiffBitmapDecoder(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat ||| BitmapCreateOptions.IgnoreImageCache,
                    BitmapCacheOption.None);   
        Seq.init (tiffDecoder.Frames.Count) (fun frameIndex ->
            let cFrame = tiffDecoder.Frames.[frameIndex]
            let convertedBitmap = new FormatConvertedBitmap(cFrame, PixelFormats.Bgr101010, null, 0.)
            let bytesPerPixel = convertedBitmap.Format.BitsPerPixel / 8
            let width  = convertedBitmap.PixelWidth
            let height = convertedBitmap.PixelHeight
            let stride = width * bytesPerPixel
            let bytes : byte[] = Array.zeroCreate (width * height * bytesPerPixel)
            convertedBitmap.CopyPixels(bytes, width * bytesPerPixel, 0)
            let pixelSize = bytesPerPixel
            Array2D.init width height (fun x y ->
                BitConverter.ToInt32 (bytes,stride * y + x * pixelSize)
                )
        )
        |> Seq.head

    ///This function takes a 2DArray. It returns a 2DArray.
    ///The returned 2DArray is padded in both dimensions with random values for the wavelet transformation.

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
            |> padArray2DWithRandom rnd 100
        paddedRawData

module Maxima =
    
    //The functions in this module are taken from BioFSharp.ImgP and modified for use in this project

    ///This function takes a MarrWavelet and a 2DArray. It returns a float 2DArray.
    ///marr is a MarrWavelet, which can be created by the marrWaveletCreator. image is the image which should be transformed with the marr wavelet.
    ///The output is the transformed image.

    let inline C3DWT (marr: MarrWavelet.MarrWavelet) (image:'a[,]) =
        //the length of both sides from the picture substracting the padding area
        let resolutionPixelfst = (Array2D.length1 image) - (100 * 2)
        let resolutionPixelsnd = (Array2D.length2 image) - (100 * 2)
        let offset = marr.PadAreaRadius
        let paddingoffset = 100
        let (CWTArray2D0: float[,]) = Array2D.zeroCreate (Array2D.length2 image) (Array2D.length1 image)
        for x = paddingoffset to (paddingoffset + (resolutionPixelsnd-1)) do
            for y = paddingoffset to (paddingoffset + (resolutionPixelfst-1)) do
                CWTArray2D0.[x,y] <-
                    let rec loop acc' a b =
                        if a <= 2 * offset then
                            if b <= 2 * offset then
                                let acc = acc' + ((marr.Values).[a,b] * (image.[(y+(a-offset)),(x+(b-offset))] |> float))
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


    ///This function takes an int and a float 2DArray. It returns a float tuple list.
    ///image is the image in which the local maxima should be found. dist is the radius for points around the checked point which should also belong to the
    ///local maximum. The returned tuple list contains the coordinates of the found maxima.

    let inline findLocalMaxima dist image =
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
        let (cWTPercArray: float [,]) = image  
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
            let boolList = surroundingList |> List.map (fun x -> isSortedAsc x)
            (boolList |> List.contains false) = false

        //calculates checkListsForContinuousDecline for every pixel
        for i=dist to (Array2D.length1 cWTPercArray)-(dist+1) do
            for j=dist to (Array2D.length2 cWTPercArray)-(dist+1) do
                if cWTPercArray.[i,j] >= 10. then
                    if checkListsForContinuousDecline i j dist = true
                        then arrayOfMaxima.[i,j] <- cWTPercArray.[i,j]
                    else arrayOfMaxima.[i,j] <- 0.
                else arrayOfMaxima.[i,j] <- 0.
        allmaximaArray arrayOfMaxima

module Filter =

    ///This function takes a float 2DArray, a float tuple and a float tuple. It returns a jagged array.
    ///image is the image which should be set to zero around a selected circle , pointAXY and pointBXY
    ///are two opposing points on the desired circle as float tuples with the X value first and the Y value second.

    let circleSelector (image: float[,]) (pointAXY: float * float) (pointBXY: float * float) =

        let jaggedPicture   = image |> Array2D.toJaggedArray

        //calculates the center point between the two given points
        let centerXY        = ((fst pointAXY + fst pointBXY)/2.,(snd pointAXY + snd pointBXY)/2.)
        //calculates the radius of the circle
        let radius          = (sqrt((fst pointAXY - fst pointBXY)**2. + (snd pointAXY - snd pointBXY)**2.))/2.
        //sets the value of every point outside of the defined circle to 0.
        let cutPicture      = jaggedPicture
                              |> Array.mapi 
                                  (fun y -> Array.mapi (fun x value->
                                   //calculates the distance of every point to the center of the cirlce. If it is larger than the radius, the value is set to 0.
                                   let distanceCenter = sqrt ((float x - fst centerXY)**2. + (float y - snd centerXY)**2.)
                                   if distanceCenter > radius then 0.
                                   else value))
        cutPicture

    ///This function takes a float 2DArray, an int tuple and an int tuple. It returns a jagged array.
    ///image is the image which should be set to zero around a selected rectangle , lowerLeftXY and lowerRightXY 
    ///are the upper left and lower right points of the rectangle as int tuples with the X value first and the Y value second.

    let rectangleSelector (image: float[,]) (upperLeftXY: int * int) (lowerRightXY: int * int) =

        let jaggedPicture = image |> Array2D.toJaggedArray

        //the four boundaries of the rectangle
        let upperY        = snd upperLeftXY
        let lowerY        = snd lowerRightXY
        let leftX         = fst upperLeftXY
        let rightX        = fst lowerRightXY
        //checks if the point is inside the boundaries, if it is not, the value is set to 0.
        let selectPicture = jaggedPicture
                            |> Array.mapi
                                (fun y -> Array.mapi (fun x value->
                                    if      y > upperY || y < lowerY then 0.
                                    elif    x > rightX || x < leftX then 0.
                                    else    value))
        selectPicture
 
    ///This function takes an int 2DArray, an int and an int. It returns an int 2DArray.
    ///image is the image which should be cut into the desired dimensions, height and width are the dimensions of the new 2DArray (picture).
    ///The center of the picture stays the same.

    let rectangleSelectorCenter (image: int[,]) (height: int) (width: int) =

        let jaggedPicture = image |> Array2D.toJaggedArray

        //calculates the center of the image
        let center        = (Array2D.length2 image) / 2, (Array2D.length1 image) / 2
        //the four boundaries of the rectangle
        let upperY        = snd center + height / 2
        let lowerY        = snd center - height / 2
        let leftX         = fst center - width / 2
        let rightX        = fst center + width / 2
        //every point outside of the boundaries is removed, resulting in a smaller picture
        let selectPicture = Array.mapi
                             (fun y array -> Array.foldi (fun x acc value->
                                 if      y > upperY || y < lowerY then acc
                                 elif    x > rightX || x < leftX then acc
                                 else    Array.append acc [|value|]) [||] array) jaggedPicture
                              |> Array.filter (fun x -> not (Array.isEmpty x))
        JaggedArray.toArray2D selectPicture

    ///This function takes a float 2DArray, a float and a boolean. It returns a jagged array.
    ///image is the image which should be thresholded, percentile is the percentage of values which should be thresholded
    ///and the boolean indicates whether the maxima are positive (true) or negative (false).

    let thresholdPercentile (image: float[,]) (percentileValue: float) (maximaPositive: bool) =

        let jaggedImage     = image |> Array2D.toJaggedArray

        if maximaPositive then
            //sorts the values in the array in an ascending order
            let percentile  = jaggedImage |> Array.concat |> Array.sort
            //cutoffValue takes the value which is higher than x % of all values
            let cutOffValue = percentile.[int (((float percentile.Length) - 1.) * percentileValue)]
            jaggedImage
            |> JaggedArray.map (fun x -> if x < cutOffValue then 0. else x)
        else
            //sorts the values in the array in an descending order
            let percentile  = jaggedImage |> Array.concat |> Array.sortDescending
            //cutoffValue takes the value which is lower than x % of all values
            let cutOffValue = percentile.[int (((float percentile.Length) - 1.) * percentileValue)]
            jaggedImage
            |> JaggedArray.map (fun x -> if x > cutOffValue then 0. else -x)


    ///This function takes a float 2DArray, a float and a boolean. It returns a jagged array.
    ///image is the image which should be thresholded, multiplier can be used to increase the cut-off value
    ///and the boolean indicates whether the maxima are positive (true) or negative (false)

    let thresholdMaxima (image: float[,]) multiplier (maximaPositive: bool) =

        let jaggedImage         = image |> Array2D.toJaggedArray

        if maximaPositive then
            //takes the maximum value of every pixel row (array) and sorts them descending
            let maxima          = jaggedImage
                                  |> Array.map Array.max
                                  |> Array.sortDescending
            //takes the highest 10% of the values and averages them
            let topTenAverage   = Array.take (maxima.Length / 10) maxima
                                  |> Array.average
            //sets every value that is lower than the cut off * multiplier to 0.
            jaggedImage
            |> JaggedArray.map (fun x -> if x < topTenAverage * multiplier then 0. else x)
        else
            //takes the minimum value of every pixel row (array) and sorts them ascending
            let minima          = jaggedImage
                                  |> Array.map Array.min
                                  |> Array.sort
            //takes the lowest 10% of the values and averages them
            let topTenAverage   = Array.take (minima.Length / 10) minima
                                  |> Array.average
            //sets every value that is higher than the cut off * multiplier to 0. and multplies it with -1 otherwise
            jaggedImage
            |> JaggedArray.map (fun x -> if x > topTenAverage * multiplier then 0. else -x)

module Pipeline =

    ///This function takes a string, an int, an int , a float and a float. It returns an int.
    ///folderpath is the path to the folder containing the images to be analyzed. height and width are the dimensions of the rectangle
    ///to be analyzed in pixels. radius is the radius of the cells that should be counted in pixels.
    ///multiplier increases or decreases the cut-off value for the thresholding function.

    let processAllImages folderPath height width radius multiplier=
        //gets a string array containing all the filenames of files in the folder
        let imagePaths        = Directory.GetFiles folderPath
        //loads the pixel values in a 2DArray
        let images            = imagePaths
                                |> Array.map Image.loadTiff
        //reduces the pictures to a rectangle with the chosen dimensions
        let selectedImages    = images
                                |> Array.map (fun x -> Filter.rectangleSelectorCenter x height width)
        //padds the images for the wavelet transformation and casts the int values in the 2DArray to floats
        let paddedImages      = selectedImages
                                |> Array.map (fun x -> Image.paddTiff (Array2D.map float x))
        //applies the wavelet transformation with a marr wavelet with chosen radius on every single point
        let transformedImages = paddedImages 
                                |> Array.mapi (fun i x -> printfn "Apply wavelet to image %i of %i" (i + 1) paddedImages.Length
                                                          Maxima.C3DWT (MarrWavelet.marrWaveletCreator radius) x
                                              )
        //sets values below or above the cut-off value to 0.
        let thresholdedImages = transformedImages
                                |> Array.map (fun x -> Filter.thresholdMaxima x multiplier false)
        //analyzes the thresholded picture for local maxima. A radius of 4 points around the local maximum is taken for the calculation.
        let localMaxima       = thresholdedImages
                                |> Array.map (fun x -> Maxima.findLocalMaxima 4 (x 
                                                                                 |> JaggedArray.transpose
                                                                                 |> JaggedArray.toArray2D
                                                                                )
                                             )
        //visual representation of the analyzing process. This part can be safely removed if not wanted
        let charts            =
            //the images are brought into the correct format and orientation for the visual representation
            let jaggedSelectedImg = selectedImages
                                    |> Array.map Array2D.toJaggedArray
                                    |> Array.map Array.transpose
            let jaggedTransfImg   = transformedImages
                                    |> Array.map Array2D.toJaggedArray
            //creates heatmaps of the original selected rectangle, the transformed version and the thresholded version
            //a point chart of the found local maxima is laid over the thresholded version to indicate found cells
            Array.mapi (fun i x -> 
                            [Chart.Combine [Chart.Point localMaxima.[i]
                                            |> Chart.withMarkerStyle (5, "black");
                                            Chart.Heatmap x
                                           ]; 
                             Chart.Heatmap jaggedTransfImg.[i];
                             Chart.Heatmap jaggedSelectedImg.[i]
                            ] 
                            |> Chart.Stack 3
                            |> Chart.withSize (1800., 600.)
                            |> Chart.Show
                ) thresholdedImages
        //an array containing the number of cells found in each image
        let cellCount = localMaxima
                        |> Array.map (fun x -> List.length x)
        cellCount