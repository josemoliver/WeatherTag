using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace WeatherTag
{
    class Program
    {
        //**
        //* Author:    José Oliver-Didier
        //* Created:   October 2018
        //* Ref: https://github.com/josemoliver/WeatherTag
        //**

        static void Main(string[] args)
        {
            
            bool writeToFile                = false;                            //flag to perform write operation to save values to image metadata.
            bool overwriteFile              = false;
            string activeFilePath           = Directory.GetCurrentDirectory();  //current file directory.
            List<WeatherReading> reading    = new List<WeatherReading>();       //weather reading values.


            //Check for Exiftool - if not found display error message.
            double exiftoolVersion = CheckExiftool();

            if (exiftoolVersion!=0)
            {
                Console.WriteLine("Exiftool version " + exiftoolVersion);
            }
            else
            {
                Console.WriteLine("Exiftool not found! WeatherTag needs exiftool in order to work properly.");
                Environment.Exit(0);
            }

            //Fetch jpg and heic files in directory, if none found display error
            string[] imageFiles = Directory.GetFiles(activeFilePath, "*.*").Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".heic", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (imageFiles.Count()==0)
            {
                Console.WriteLine("No supported image files found.");
                Environment.Exit(0);
            }
            
            string weatherHistoryFile = Directory.GetCurrentDirectory() + "\\weatherhistory.csv"; //Get weather history csv file

            
            //Check for -write flag, if found then matched weather values will be writen back to the jpg file EXIF metadata.
            for (int i=0; i<args.Count();i++)
            {
                if (args[i].ToString().ToLower().Trim()=="-write")
                {
                    writeToFile = true;
                }

                if (args[i].ToString().ToLower().Trim() == "-overwrite")
                {
                    overwriteFile = true;
                }
            }

            if (writeToFile==false)
            {
                Console.WriteLine("No changes to file(s) will be performed - To write weather tags use -write flag");
            }

                        
            //Load weather history file into stream
            try
            {
                using (var reader = new StreamReader(weatherHistoryFile))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        var values = line.Split(',');

                        Double ambientTemperature = 99999;
                        Double humidity = 99999;
                        Double pressure = 99999;

                        DateTime dateWeatherReading = DateTime.Parse(values[0].ToString().Trim() + " " + values[1].ToString().Trim());


                        //Load Ambient Temperature value, if invalid mark as invalid = 99999
                        try
                        {
                            ambientTemperature = Double.Parse(RemoveNonNumeric(values[2].ToString().Trim()));

                            //Check for valid Ambient Temperature Range in Celsius
                            if ((ambientTemperature < -100) || (ambientTemperature > 150))
                            {
                                ambientTemperature = 99999;
                            }

                        }
                        catch
                        {
                            ambientTemperature = 99999;
                        }

                        //Load Humidity value, if invalid mark as invalid = 99999
                        try
                        {
                            humidity = Double.Parse(RemoveNonNumeric(values[3].ToString().Trim()));

                            //Check for valid Humidity Range
                            if ((humidity < 0) || (humidity > 100))
                            {
                                humidity = 99999;
                            }

                        }
                        catch
                        {
                            humidity = 99999;
                        }
                        try
                        {
                            pressure = Double.Parse(RemoveNonNumeric(values[4].ToString().Trim()));

                            //Check for valid Pressure Range
                            if ((pressure < 800) || (pressure > 1100))
                            {
                                pressure = 99999;
                            }
                        }
                        catch
                        {
                            pressure = 99999;
                        }

                        reading.Add(new WeatherReading(dateWeatherReading, ambientTemperature, humidity, pressure));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(weatherHistoryFile + " not found or unable to open.");
                Console.WriteLine(e.ToString());
                Environment.Exit(0);
            }


            //For each image file in the folder, add the closest reading from the weather history file.
            for (int q = 0; q < imageFiles.Count(); q++)
            {

                DateTime photoDate;
                bool noPhotoDate = false;

                //Get the image's Created Time, if not found or an error occurs set to error value of 1/1/2050 12:00 AM
                try
                {
                    photoDate = GetFileDate(imageFiles[q]);
                }
                catch
                {
                    photoDate = DateTime.Parse("1/1/2050 12:00 AM");
                    noPhotoDate = true;
                }

                Double minDiffTime = 30;
                WeatherReading closestReading = new WeatherReading(DateTime.Parse("1/1/1900 12:00 AM"), 0, 0, 0);

                if (noPhotoDate == false)
                {
                    for (int i = 0; i < reading.Count; i++)
                    {
                        TimeSpan diffTime = photoDate - reading[i].ReadingDate;
                        if (Math.Abs(diffTime.TotalMinutes) < minDiffTime)
                        {
                            closestReading = reading[i];
                            minDiffTime = Math.Abs(diffTime.TotalMinutes);
                        }
                    }
                }


                Console.WriteLine("------ File " + (q+1).ToString()+ " of " + imageFiles.Count() + " ------");
                Console.WriteLine(minDiffTime.ToString());

                if (minDiffTime < 30)
                {

                    string consoleOutput = "";

                    if (closestReading.AmbientTemperature != 99999)
                    {
                        consoleOutput = consoleOutput + " " + closestReading.AmbientTemperature.ToString() + " °C ";
                    }
                    else
                    {
                        consoleOutput = consoleOutput + " -- °C ";
                    }

                    if (closestReading.Humidity != 99999)
                    {
                        consoleOutput = consoleOutput + " " + closestReading.Humidity.ToString() + " %";
                    }
                    else
                    {
                        consoleOutput = consoleOutput + " -- % ";
                    }

                    if (closestReading.Pressure != 99999)
                    {
                        consoleOutput = consoleOutput + " " + closestReading.Pressure.ToString() + " hPa";
                    }
                    else
                    {
                        consoleOutput = consoleOutput + " -- hPa ";
                    }

                    Console.WriteLine(imageFiles[q].ToString().Replace(Directory.GetCurrentDirectory(),"").Trim()+" - "+ consoleOutput);
                    if (writeToFile == true)
                    {
                        string WriteStatus = WriteFileInfo(imageFiles[q], closestReading, overwriteFile);
                        Console.WriteLine(WriteStatus);
                    }
                   
                }
                else
                {
                    if (noPhotoDate == true)
                    {
                        Console.WriteLine(imageFiles[q].ToString().Replace(Directory.GetCurrentDirectory(), "").Trim() + " - Photo file has no date and time.");
                    }
                    else
                    {
                        Console.WriteLine(imageFiles[q].ToString().Replace(Directory.GetCurrentDirectory(), "").Trim() + " - No reading found.");
                    }
                }
            }

            Console.WriteLine();

        }

        static string RemoveNonNumeric(string input) { return Regex.Replace(input, @"[^0-9\.\-]", ""); }

        public static DateTime GetFileDate(string file)
        {
            //Retrieve Image Date
            List<ExifToolJSON> ExifToolResponse;
            string createDateTime = "";
            string createDate = "";
            string createTime = "";

            // Start Process
            Process p = new Process();

            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Arguments = "\"" + file + "\" -CreateDate -mwg -json";
            p.StartInfo.FileName = "exiftool.exe";
            p.Start();


            // Read the output stream 
            string json = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            
            if (json != "")
            {
                ExifToolResponse = JsonConvert.DeserializeObject<List<ExifToolJSON>>(json);
                createDateTime = ExifToolResponse[0].CreateDate.ToString().Trim();
                string[] words = createDateTime.Split(' ');
                createDate = words[0].ToString().Replace(":","/");
                createTime = words[1].ToString();
                createDateTime = createDate + " " + createTime;
            }
            
            return DateTime.Parse(createDateTime);
        }

        public static double CheckExiftool()
        {

            double exiftoolreturn = 0;

            try
            { 

            // Start Process
                Process p = new Process();

            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Arguments = "-ver";
            p.StartInfo.FileName = "exiftool.exe";
            p.Start();

            
            // Read the output stream
           
                string exiftooloutput = p.StandardOutput.ReadToEnd().Trim();
                exiftoolreturn = double.Parse(exiftooloutput);
                p.WaitForExit(8000);
           }
           catch
            {
               exiftoolreturn = 0;
            }

            return exiftoolreturn;
        }

        public static string WriteFileInfo(string File, WeatherReading reading, bool overwriteFile)
        {
            //Write Weather Values back to file

            string output = "";
            // Start the child process.
            Process p = new Process();

            string Arguments = "";

            if (reading.AmbientTemperature != 99999)
            {
                Arguments = Arguments + " -\"AmbientTemperature=" + reading.AmbientTemperature + "\"";
            }
            if (reading.Humidity != 99999)
            {
                Arguments = Arguments + " -\"Humidity=" + reading.Humidity + "\"";
            }
            if (reading.Pressure != 99999)
            {
                Arguments = Arguments + " -\"Pressure=" + reading.Pressure + "\"";
            }
            if (overwriteFile==true)
            {
                Arguments = Arguments + " -overwrite_original";
            }


            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Arguments = "\""+File+"\"" + Arguments;
            p.StartInfo.FileName = "exiftool.exe";
            p.Start();
            output = File + Arguments+" --- " + p.StandardOutput.ReadToEnd();
            p.WaitForExit(10000);

            return output;
        }

        public class ExifToolJSON
        {
            public string SourceFile { get; set; }
            public string CreateDate { get; set; }
        }

        
        public class WeatherReading
        {
            public WeatherReading(DateTime readingDate, double ambientTemperature, double humidity, double pressure)
            {
                this.ReadingDate = readingDate;
                this.AmbientTemperature = ambientTemperature;
                this.Humidity = humidity;
                this.Pressure = pressure;
            }

            public DateTime ReadingDate { get; set; }
            public double AmbientTemperature { get; set; }
            public double Humidity { get; set; }
            public double Pressure { get; set; }
        }
    }
}

