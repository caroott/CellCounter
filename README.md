This project aims to automatically count cells in given pictures. Its main use is the counting of
cell cultures utilizing improved Neubauer counting chambers.

Usefull links:

* Short overview of the project : https://joott.github.io/CellCounter/
* Explanation of Functions      : https://joott.github.io/CellCounter/FunctionExplanation.html
* Usage example                 : https://joott.github.io/CellCounter/DilutionExperiment.html



## Installation

1. [Click here](https://code.visualstudio.com/download) to download VisualStudioCode
2. [Click here](https://git-scm.com/download/win) to download Git
3. [Click here](https://dotnet.microsoft.com/download) to download .NET Core SDK AND .NET Framework Dev Pack
4. [Click here](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2017) to download Build Tools for Visual Studio 2019 
	While installing check the boxes for:
	Under "Workload":
	- .NET Core build tools
	Under "Individual components":
	- NuGet targets and build tasks
	- NuGet package manager
	- .NET Framework 4.7 SDK
	- .NET Framework 4.7 targeting pack
	- F# compiler
	...then install
5. Restart your computer
6. Install fake cli. Open [command prompt](https://en.wikipedia.org/wiki/Command-line_interface)(console) by searching in the windows search bar for "cmd" and type in the new window "dotnet tool install fake-cli -g" (without the quotation marks)
7. [Click here](https://github.com/Joott/CellCounter/archive/master.zip) or scroll up to download either master or developer branch of this repository.
	Master branch should be a fully functionable variant, while the developer branch often has more features which are not fully tested yet.
	At this point i recommend downloading the developer branch, as it will be updated the most.
	Unzip the file in any folder, except the Desktop!
8. Open command prompt(console) and navigate to the Folder _(Copy path to this folder)_ with the build.cmd inside. 
		_(console command: cd __PathToYourFolder__)_
9. Console command: fake build
10. Install Ionide in visual studio code:
	(_open visual studio code -> Extensions -> type in Ionide-fsharp -> install_)
11. Thats finally it, you can now go and reference the CellCounter.dll for your use.

If you need any other information you can take a look at the [F# Software Foundation](https://fsharp.org/)
