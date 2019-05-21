(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#r @"C:\Users\Ott\source\repos\CellCounter\lib\Formatting\FSharp.Plotly.dll"
#r @"C:\Users\Ott\source\repos\CellCounter\src\CellCounter\bin\Release\netstandard2.0\CellCounter.dll"
#r @"C:\Users\Ott\source\repos\CellCounter\packages\NETStandard.Library\build\netstandard2.0\ref\netstandard.dll"

open FSharp.Plotly
open CounterFunctions

(**
Calculating Dimensions
----------------------
The functions in this library require two different dimensions in pixels. Since pixels are not "real" parameters, here are two functions to calculate
the dimensions from parameters given by the equipment.

The first parameter needed is the dimension of a square under the counting chamber. The function to calculate that for an improved Neubauer counting chamber is:
*)
Filter.squareCalculator cameraPixelSize binning magnification cameraMount
(**
Camera pixel size is usually given in A x A microns. A is inserted as a parameter for the function. Binning is also mostly given in a B x B format, where a B
is the parameter taken. Magnification represents the magnification of the microscope, usually a 20x or 40x. Camera mount is an additional possible source of magnification,
mostly a 1x, but sometimes a different modification and has to be taken into account. This function fills the height and width part of the processImage function.

###Important note about the pictures
The pictures are taken from a part of the counting chamber, where no lines are present. The function automatically takes a part from the middle of the picture with a size corresponding to
the squares of the counting chamber. Therefore no lines are needed and only influence the result negatively!

The second parameter needed is the radius for the wavelet function in pixels. It corresponds to the diameter of the cells to be counted. The function takes
nearly the same input as the previous one, except that its first parameter is the diameter of the cells in microns.
*)
Filter.cellRadiusCalculator cellDiameter cameraPixelSize binning magnification cameraMount
(**
Processing Images
-----------------
The function processImage is a function which combines all processing functions in the correct order for one single image and analyzes a square in the center of the picture with the size of
a square from the counting chamber. It also serves rather as an example of how to use the functions, since it can be modified in several ways, which are detailed below.
*)
Pipeline.processImage filePath height width radius thresholdMultiplier
(**
filePath is the path of the image. Height and width can be filled with the same result from the Filter.squareCalculator. Radius gets the result from Filter.cellRadiusCalculator.
thresholdMultiplier is a multiplier for the cutoff after the wavelet transformation. It has to be higher the closer in intensity the background to the cells is. The threshold function in the
pipeline contains a boolean for determining whether the cells have a positive or negative value. In all cases so far the intensities were negative. If for some reason they are positive,
the boolean has to be changed to "true". The chart which is returned in the resulting tuple serves visualization purposes. This functionality can be removed without consequences. To view the chart
you have to open FSharp.Plotly and use the command Chart.Show.
*)
Pipeline.processImages folderPath height width radius thresholdMultiplier
(**
This function is a convenience function which applies the previous function to a whole folder with images. It takes a folderpath instead of the filepath and processes all
images in that folder.

Possible Variants
----------------

The threshold function currently used in the Pipeline.processImage is based on the maximum intensities in the pictures. An alternative threshold function exists which is based on a percentile cutoff. It can be used
in place of the other function, although its performance is worse with changing cell numbers.

To count cells in petri dishes, the Filter.rectangleSelectorCenter function in the pipeline has to be replaced with:
*)
Filter.circleSelector pointAXY pointBXY
(**
This function takes two opposing points on a circle in the format (X-coordinate1, Y-coordinate1) (X-coordinate2, Y-coordinate2).
Those coordinates can be read from the picture directly. This input would then replace height and width. If for some reason the square shouldn't be in the middle
of the picture, following function can be used:
*)
Filter.rectangleSelector upperLeftXY lowerRightBXY
(**
It has a similar input format as the Filter.circleSelector, except it takes the upper left and lower right points of the rectangle.

Misc
----
The functions in the modules Maxima and Image are adapted from the [BioFSharp](https://github.com/CSBiology/BioFSharp) library, an open source bioinformatics toolbox written in F#.
The functions were modified to fit the needs of the Cell Counter.
*)
