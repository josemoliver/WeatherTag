# Weather Tag

## Introduction

This is a simple Windows console (Command Line) application for adding weather values to a set of jpg image file EXIF 2.31 Metadata. Using the date and time a photo was taken, the app matches the closest weather reading from a file containing periodic weather readings weatherhistory.csv file.

The idea behind this utility is if you can obtain historical weather information for a location near where the photos were taken this information could be saved within the image file metadata.

Read the blog post: [Capturing the moment and the ambient weather information in photos](https://jmoliver.wordpress.com/2018/07/07/capturing-the-moment-and-the-ambient-weather-information-in-photos/)

## Dependency with ExifTool

WeatherTag requires that **ExifTool.exe** be copied into the same folder with WeatherTag.exe on your PC. ExifTool is a utility written by Phil Harvey which can read, write and edit file metadata. Exiftool can be obtained at https://www.sno.phy.queensu.ca/~phil/exiftool/

## Installation

1. On your PC create a folder called "WeatherTag"
2. Copy the WeatherTag.exe file to the folder WeatherTag
3. Copy Exiftool.exe to the Weather Tag folder.
4. Add the Weather Tag folder to the Windows  **path**  variable. If you are not familiar on how to add a folder to the Windows **path**  variable. Search the web for "Add folder to Environment path" for detailed descriptions as it may vary between Windows versions.

## Usage
There are two pieces of information required for Weather Tag to perform its function:
1. Any number of photos taken with correct Date and Time stamp. 
2. A comma separated value file (.csv) in UTF-8 named **weatherhistory.csv** containing periodic weather measurements. The first column values are required to contain a date in MM/DD/YYYY format (Example: 10/5/2018). The second column values are required to contain time values in 12-Hour format (Example: 3:00 PM). The third column should contain ambient temperature values in Celsius. The fourth column values should contain atmospheric humidity in percentage. The fifth column values should contain atmospheric pressure in hectopascals (hPa). The Ambient Temperature, Humidity and Pressure values need not have the measurement units added (°C, %, hPa). 

When WeatherTag.exe is executed within a folder containing image files and the weatherhistory.csv file it will attempt to match the closest weather reading (within an hour) for the date and time a photo was taken. If the **-write** flag is use it will then write the Ambient Temperature, Humidity and Pressure values to the image files' corresponding EXIF metadata fields.

## Example Files
Within the **example** folder there are 5 sample images along with a historical weather log from a weather station near the location where the photos were taken. Open a Command Prompt within the example folder. Running **WeatherTag.exe** within the folder will match the closest weather measurement contained. Running **WeatherTag.exe -write** will match the weather measurements as well as write the information back to the photo image files. A copy of the original image file will be made with the ***.jpg_original** extension. If you wish to delete the original image files and keep the modified files you can delete them by using the **del *.jpg_original** command. 

## Download WeatherTag.exe
You can download WeatherTag.exe from the project's GitHub Release page - https://github.com/josemoliver/WeatherTag/releases 

## Build WeatherTag.exe
1. Open the **WeatherTag.sln file** in Visual Studio 2017. 
2. WeatherTag uses Newtonsoft.JSON Nuget Package which should be downloaded using the Nuget Package Manager.
3. Build Solution <Ctrl>+<Shift>+<B>
4. The **WeatherTag.exe** file should be deposited in the **bin** folder. 
