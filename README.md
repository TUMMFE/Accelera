# Acceleration Sensor Project
The Acceleration Sensor Project which is used to measure the acceleration of the thumb after transcranial magnetic stimulation.

The project consists of the following parts:
* Hardware
* Firmware
* PC Software
* Experiments

## Software (PC Host)

### Features
- continous data aquisition mode
- acoustic stimulation mode
- full configuration of adxl355 sensor
- save data files as CSV data with additional subject information as well as with a time mark file to create real time axis in acoustic stimulation mode
- open data files an show absolute acceleration as well as x,y and z-directions
- real time plot view function (shortcuts can be found [here](https://stackoverflow.com/questions/27144051/which-keyboard-shortcut-functions-are-already-implemented-in-oxyplot))


### Prerequisites & How To Compile
Software is written in C# using Visual Studio Community 2022

*Prerequisites*
- .Net Framework 4.8
- Inno Setup Compiler 5.5.9 (to create the installation package)
- WebUpdater/CreateXmlForms (to create the update XML file)
- YALV ([Link](https://github.com/LukePet/YALV)) (log file viewer)

*Packages*
- CsvHelper
- log4net
- MathNet.Filtering
- MathNet.Numerics
- Microsoft.NETFramework.ReferenceAssemblies
- Microsoft.NETFramework.ReferenceAssemblies.net472
- Microsoft.Toolkit.Uwp.Notifications
- Microsoft.Windows.SDK.Contracts
- Newtonsoft.Json
- Ookii.Dialogs.Wpf
- OxyPlot.Wpf
- OxyPlot.Core
- SSH.NET

*Visual Studio Extensions*
- Codist for enhanced syntax highlighting
- Automatic Version 3 for increasing of version numbers

*Version Numbering*
Use the following scheme for version settings:

Major    | Minor   | Build   | Revision
---------|---------|---------|-------------
on demand|on demand|on demand|data (yyddd)


*How To Compile*
- switch to release
- rebuild the project

*How To Create the Installation Package*
- open `setup_accelera.iss` with the Inno Setup Compiler
- build the setup project
- `setup_accelera.exe`is in the `OutPut` folder (top level of the whole project)

*How To Create A Web Update*
- Compile the project
- add the description to the file `changelog.txt` in the folder `InnoSetup`
- create the Installation package
- run/install the WebUpdater Tool from `\Tools\WebUpdate\`
- **Path to the application file**: select the latest accelera.exe file (usually in the folder `Accelera\Accelera\bin\Release`)
- **Path to the file used for downloading**: select the latest `setup_accelera.exe` file (usually in the folder `Output`)
- add a description for the update which will be shown during the update process
- click `create UPDATE.XML`
- Upload (using e.g. FileZilla) both files (`setup_accelera.exe` and `update.xml`) to the FTP drive. Use the following credentials for that
	+ Protocoll: SFTP
	+ Server: home16315215.1and1-data.host
	+ User: u1878379852
	+ Password: vMahr2gz3dWAaV9ETXm5


## Authors & Contribution

What         |Who
-------------|---------------
Firmware     |Bernhard Gleich
Software     |Bernhard Gleich
Hardware     |Bojan Sandurkov
Data analysis|Jonathan Rapp
Experiments  |----
