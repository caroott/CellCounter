(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
CellCounter
======================

Introduction
-------

This application contains functions to automatically count cells from pictures taken. The intention behind the application
is to analyze pictures of cells taken under a microscope from a [Neubauer counting chamber](https://en.wikipedia.org/wiki/Hemocytometer)
to quickly calculate cell numbers for the colonies in question. While it is intended for pictures of a counting chamber, it can also be used to count colonies
inside a petri dish, but this use needs more user input and calibration and results vary depending on the quality of the image taken.

The idea for this counter is to apply a [Marr wavelet](https://en.wikipedia.org/wiki/Mexican_hat_wavelet), the 2D version of the
Mexican hat wavelet, to the part of the image which should be analyzed. The effect of the wavelet is, that it enhances the parts of the picture
which fit the shape of the wavelet, making it easier to distinguish those parts from the rest of the image.
Because of its circular shape, it fits the form of cells really well, making it a good candidate for this application.
For more informations about wavelets and their function, i can recommend this [Tutorial](http://users.rowan.edu/~polikar/WTtutorial.html)



Example
-------

This is an example of an analyzed picture. On the left you can see the end result, where all values below a certain threshold have been removed and the maxima have been located (black dots).
In the middle is the picture after the wavelet transformation, and on the right is the original picture. The pictures are shown as heatmaps.

![Plot]("C:\Users\Student\source\repos\CellCounter\docsrc\files\img\Capture.PNG")

Samples & documentation
-----------------------

The library comes with comprehensible documentation. 
It can include tutorials automatically generated from `*.fsx` files in [the content folder][content]. 
The API reference is automatically generated from Markdown comments in the library implementation.

 * [Tutorial](tutorial.html) contains a further explanation of this sample library.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsprojects/CellCounter/tree/master/docs/content
  [gh]: https://github.com/fsprojects/CellCounter
  [issues]: https://github.com/fsprojects/CellCounter/issues
  [readme]: https://github.com/fsprojects/CellCounter/blob/master/README.md
  [license]: https://github.com/fsprojects/CellCounter/blob/master/LICENSE.txt
*)
