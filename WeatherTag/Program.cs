using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace WeatherTag
{
    class Program
    {
        
        static void Main(string[] args)
        {
            
            bool WriteToFile = false;
            string ActiveFilePath = Directory.GetCurrentDirectory();
            List<WeatherReading> reading = new List<WeatherReading>();


            //Check for Exiftool - if not found display error message.
            double ExiftoolVersion = CheckExiftool();

            if (ExiftoolVersion!=0)
            {
                Console.WriteLine("Exiftool version " + ExiftoolVersion);
            }
            else
            {
                Console.WriteLine("Exiftool not found! WeatherTag needs exiftool in order to work properly.");
                Environment.Exit(0);
            }

            //Fetch jpg files in directory, if none found display error
            string[] ImageFiles = Directory.GetFiles(ActiveFilePath, "*.jpg");

            if (ImageFiles.Count()==0)
            {
                Console.WriteLine("No .jpg files found.");
                Environment.Exit(0);
            }
            
            string WeatherHistoryFile = Directory.GetCurrentDirectory() + "\\weatherhistory.csv";

            
            //Check for -write flag, if found then matched weather values will be writen back to the jpg file EXIF metadata.
            for (int i=0; i<args.Count();i++)
            {
                if (args[i].ToString().ToLower().Trim()=="-write")
                {
                    WriteToFile = true;
                }
            }

            if (WriteToFile==false)
            {
                Console.WriteLine("No changes to file(s) will be performed - To write weather tags use -write flag");
            }

            
            //Console.WriteLine("Weather file: " + WeatherHistoryFile);

            //Load weather history file into stream
            try
            {
                using (var reader = new StreamReader(WeatherHistoryFile))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        //Remove any measurement symbols
                        line = line.Replace("°", "");
                        line = line.Replace("C", "");
                        line = line.Replace("hPa", "");
                        line = line.Replace("%", "");

                        var values = line.Split(',');

                        Double ambientTemperature = 99999;
                        Double humidity = 99999;
                        Double pressure = 99999;

                        DateTime Date1 = DateTime.Parse(values[0].ToString().Trim() + " " + values[1].ToString().Trim());


                        //Load Ambient Temperature value, if invalid mark as invalid = 99999
                        try
                        {
                            ambientTemperature = Double.Parse(values[2].ToString().Trim());

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
                            humidity = Double.Parse(values[3].ToString().Trim());

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
                            pressure = Double.Parse(values[4].ToString().Trim());

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

                        reading.Add(new WeatherReading(Date1, ambientTemperature, humidity, pressure));
                    }
                }
            }
            catch
            {
                Console.WriteLine(WeatherHistoryFile +" not found or unable to open.");
                Environment.Exit(0);
            }


            //For each image file in the folder, add the closest reading from the weather history file.
            for (int q = 0; q < ImageFiles.Count(); q++)
            {

                DateTime PhotoDate;
                bool NoPhotoDate = false;

                //Get the image's Created Time, if not found or an error occurs set to error value of 1/1/2050 12:00 AM
                try
                {
                    PhotoDate = GetFileDate(ImageFiles[q]);
                }
                catch
                {
                    PhotoDate = DateTime.Parse("1/1/2050 12:00 AM");
                    NoPhotoDate = true;
                }

                Double MinDiffTime = 30;
                WeatherReading closestReading = new WeatherReading(DateTime.Parse("1/1/1900 12:00 AM"), 0, 0, 0);

                if (NoPhotoDate == false)
                {
                    for (int i = 0; i < reading.Count; i++)
                    {
                        TimeSpan DiffTime = PhotoDate - reading[i].ReadingDate;
                        if (Math.Abs(DiffTime.TotalMinutes) < MinDiffTime)
                        {
                            closestReading = reading[i];
                            MinDiffTime = Math.Abs(DiffTime.TotalMinutes);
                        }
                    }
                }


                Console.WriteLine("------ File " + (q+1).ToString()+ " of " + ImageFiles.Count() + " ------");

                if (MinDiffTime < 30)
                {

                    string ConsoleOutput = "";

                    if (closestReading.AmbientTemperature != 99999)
                    {
                        ConsoleOutput = ConsoleOutput + " " + closestReading.AmbientTemperature.ToString() + "°C ";
                    }
                    else
                    {
                        ConsoleOutput = ConsoleOutput + " -- °C ";
                    }

                    if (closestReading.Humidity != 99999)
                    {
                        ConsoleOutput = ConsoleOutput + " " + closestReading.Humidity.ToString() + " %";
                    }
                    else
                    {
                        ConsoleOutput = ConsoleOutput + " -- % ";
                    }

                    if (closestReading.Pressure != 99999)
                    {
                        ConsoleOutput = ConsoleOutput + " " + closestReading.Pressure.ToString() + " hPa";
                    }
                    else
                    {
                        ConsoleOutput = ConsoleOutput + " -- hPa ";
                    }


                    Console.WriteLine(ImageFiles[q].ToString().Replace(Directory.GetCurrentDirectory(),"").Trim()+" - "+ConsoleOutput);
                    if (WriteToFile == true)
                    {
                        string WriteStatus = WriteFileInfo(ImageFiles[q], closestReading);
                        Console.WriteLine(WriteStatus);
                    }
                   
                }
                else
                {
                    if (NoPhotoDate == true)
                    {
                        Console.WriteLine(ImageFiles[q].ToString().Replace(Directory.GetCurrentDirectory(), "").Trim() + " - Photo file has no date and time.");
                    }
                    else
                    {
                        Console.WriteLine(ImageFiles[q].ToString().Replace(Directory.GetCurrentDirectory(), "").Trim() + " - No reading found.");
                    }
                }
            }

            Console.WriteLine();

        }

        public static DateTime GetFileDate(string file)
        {
            //Retrieve Image Date

            List<ExifToolJSON> ExifToolResponse;
            string CreateDateTime = "";
            string CreateDate = "";
            string CreateTime = "";

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
                CreateDateTime = ExifToolResponse[0].CreateDate.ToString().Trim();
                string[] words = CreateDateTime.Split(' ');
                CreateDate = words[0].ToString().Replace(":","/");
                CreateTime = words[1].ToString();
                CreateDateTime = CreateDate + " " + CreateTime;
            }
            
            return DateTime.Parse(CreateDateTime);
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

        public static string WriteFileInfo(string File, WeatherReading reading)
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

