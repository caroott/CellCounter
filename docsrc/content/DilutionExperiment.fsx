(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#r @"C:\Users\Student\source\repos\CellCounter\lib\Formatting\FSharp.Plotly.dll"
#r @"C:\Users\Student\source\repos\CellCounter\src\CellCounter\bin\Release\netstandard2.0\CellCounter.dll"
#r @"C:\Users\Student\source\repos\CellCounter\packages\NETStandard.Library\build\netstandard2.0\ref\netstandard.dll"

open FSharp.Plotly
open CounterFunctions
open System.IO

let cellCounter         = [|13500000.; 7500000.; 3722222.; 1527778.; 1027778.|]
let cellCounterStabw    = [|180021.; 245327.; 501541.; 171234.; 306816.|]
let manual              = [|14000000.; 6638889.; 3222222.; 1611111.; 944444.|]
let manualStabw         = [|784691.; 519675.; 306816.; 238953.; 398686.|]
let coulterCounter      = [|13113333.; 7283333.; 3813000.; 1968333.; 1006067.|]
let coulterCounterStabw = [|744058.; 59668.; 46755.; 18553.; 15179.|]
let increments          = [|"1:1"; "1:2"; "1:4"; "1:8"; "1:16"|]



let xAxis title = Axis.LinearAxis.init(Title=title,Showgrid=false,Showline=true,Mirror=StyleParam.Mirror.All,Zeroline=false,Tickmode=StyleParam.TickMode.Auto,Ticks= StyleParam.TickOptions.Inside, Tickfont=Font.init(StyleParam.FontFamily.Arial,Size=20.),Titlefont=Font.init(StyleParam.FontFamily.Arial,Size=20.))

let yAxis title = Axis.LinearAxis.init(Title=title,Showgrid=false,Showline=true,Mirror=StyleParam.Mirror.All,Zeroline=false,Tickmode=StyleParam.TickMode.Auto,Ticks= StyleParam.TickOptions.Inside,Tickfont=Font.init(StyleParam.FontFamily.Arial,Size=20.),Titlefont=Font.init(StyleParam.FontFamily.Arial,Size=20.))


let chartCellCounter =
    Chart.Line (increments, cellCounter)
    |> Chart.withYErrorStyle cellCounterStabw
    |> Chart.withTraceName "Cell Counter"

let chartManual =
    Chart.Line (increments, manual)
    |> Chart.withYErrorStyle manualStabw
    |> Chart.withTraceName "Manual counting"

let chartCoulterCounter =
    Chart.Line (increments, coulterCounter)
    |> Chart.withYErrorStyle coulterCounterStabw
    |> Chart.withTraceName "Coulter Counter"

let combinedChart =
    [
        chartCellCounter;
        chartManual;
        chartCoulterCounter
    ]
    |> Chart.Combine
    |> Chart.withX_Axis (xAxis "Dilution")
    |> Chart.withY_Axis (yAxis "Cells/ml")
    |> Chart.withSize (900., 900.)

(**
Counting results
----------------

To determine the effectiveness of the Cell Counter, I compared it to two different other counting methods. One of them was manual counting using the 
[Neubauer counting chamber](https://en.wikipedia.org/wiki/Hemocytometer), the other was counting the cells with a [Coulter Counter](https://en.wikipedia.org/wiki/Coulter_counter).
For that i took a cell suspension with ~1.35 * 10^7 cells and set up a serial dilution by always halving the amount up to 1:16.
I took 3 measurements for each timepoint. One measurement for both, the Cell Counter and the manual counting consists of three seperate images.

Here you can see the result of those measurements:
*)
(*** include-value:combinedChart ***)


(**
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
</br>
The Y-error bar represents the standard deviation of the 3 measurements taken at each dilution step.

As you can see in this little test, all three methods give comparable results regarding the amount of cells/ml. While the manual counting of cells is
a tedious approach, the Cell Counter has it's merits in efficiency compared to the Coulter Counter. All it needs are pictures taken under a microscope, which 
is easily done, even more so if it is done for more than one culture at once since it is only swapping of objectives then. It is also usefull for the
cell counting in bioreactors. Bioreactors have to be monitored constantly for possible infections under the microscope with picture documentation, so one can use those
pictures as input for the Cell Counter at the same time.

Experiment Data
---------------

For those of you, who want to replicate the experiment, here are the parameters used in the functions and the picture data:

Camera and microscope

* Pixel Size     : 6.45x6.45 microns
* Binning        : 2x2
* Camera Mount   : 2x
* Magnification  : 20x

Cells

* Diameter: 9.5 microns

[Here](https://1drv.ms/f/s!Al3ycUpvciEdkOEhELLpMhTADfLLvA) are the pictures for the program to analyze.

The corresponding code looks like this:
*)
let dimension = Filter.squareCalculator 6.45 2. 20. 2.

let wvRadius = Filter.cellRadiusCalculator 9.5 6.45 2. 20. 2.

let processFolders folderPath height width radius multiplier = 
    let subfolders  = Directory.GetDirectories folderPath
    let result      = subfolders
                      |> Array.mapi (fun i folder ->
                            printfn "processing folder %i of %i" (i+1) subfolders.Length
                            Pipeline.processImages folder height width radius multiplier) 
    result

let processAll = processFolders @"...\Dilution\CoulterCounter"
                    dimension dimension wvRadius 0.2

(**
The data for the manual counting can be found [here](https://1drv.ms/f/s!Al3ycUpvciEdkOEn8fGtLj7uMFQp4A), and the Coulter Counter data is [here](https://1drv.ms/f/s!Al3ycUpvciEdkOEgEXHeieR94E2GGw).

A more in depth explanation how to adapt the functions for your use can be found under...
*)



