using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Text;

namespace FileSynchronization
{
    /// <summary>
    /// Class to store one CSV row
    /// </summary>
    public class CsvRow : List<string>
    {
        public string LineText { get; set; }
        internal static char _delimiter = ',';
        
    }

    /// <summary>
    /// Class to write data to a CSV file
    /// </summary>
    public class CsvFileWriter : StreamWriter
    {
        
        public CsvFileWriter(Stream stream)
            : base(stream)
        {
        }

        public CsvFileWriter(string filename)
            : base(filename)
        {
        }

        /// <summary>
        /// Writes a single row to a CSV file.
        /// </summary>
        /// <param name="row">The row to be written</param>
        public void WriteRow(CsvRow row)
        {
            StringBuilder builder = new StringBuilder();
            bool firstColumn = true;
            foreach (string value in row)
            {
                // Add separator if this isn't the first value
                if (!firstColumn)
                    builder.Append(CsvRow._delimiter);
                // Implement special handling for values that contain comma or quote
                // Enclose in quotes and double up any double quotes
                if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
                    builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                else
                    builder.Append(value);
                firstColumn = false;
            }
            row.LineText = builder.ToString();
            WriteLine(row.LineText);
        }
    }

    /// <summary>
    /// Class to read data from a CSV file
    /// </summary>
    public class CsvFileReader : StreamReader
    {
        public CsvFileReader(Stream stream)
            : base(stream)
        {
        }

        public CsvFileReader(string filename)
            : base(filename)
        {
        }

        /// <summary>
        /// Reads a row of data from a CSV file
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool ReadRow(CsvRow row)
        {
            row.LineText = ReadLine();
            
            if (String.IsNullOrEmpty(row.LineText))
                return false;

            int pos = 0;
            int rows = 0;

            while (pos < row.LineText.Length)
            {
                string value;

                // Special handling for quoted field
                if (row.LineText[pos] == '"')
                {
                    // Skip initial quote
                    pos++;

                    // Parse quoted value
                    int start = pos;
                    while (pos < row.LineText.Length)
                    {
                        // Test for quote character
                        if (row.LineText[pos] == '"')
                        {
                            // Found one
                            pos++;

                            // If two quotes together, keep one
                            // Otherwise, indicates end of value
                            if (pos >= row.LineText.Length || row.LineText[pos] != '"')
                            {
                                pos--;
                                break;
                            }
                        }
                        pos++;
                    }
                    value = row.LineText.Substring(start, pos - start);
                    value = value.Replace("\"\"", "\"");
                }
                else
                {
                    // Parse unquoted value
                    int start = pos;
                    while (pos < row.LineText.Length && row.LineText[pos] != CsvRow._delimiter)
                        pos++;
                    value = row.LineText.Substring(start, pos - start);
                }

                // Add field to list
                if (rows < row.Count)
                    row[rows] = value;
                else
                    row.Add(value);
                rows++;

                // Eat up to and including next comma
                while (pos < row.LineText.Length && row.LineText[pos] != CsvRow._delimiter)
                    pos++;
                if (pos < row.LineText.Length)
                    pos++;
            }
            // Delete any unused items
            while (row.Count > rows)
                row.RemoveAt(rows);

            // if last column contains empty string, row intance is known to contain only 
            if (row.LineText[pos - 1] == CsvRow._delimiter)
            {
                row.Add("");
            }

            // Return true if any columns read
            return (row.Count > 0);
        }
    }

    public static class CSVHelper
    {
        
        static readonly AppSettingsReader configReader = new AppSettingsReader();
        private static readonly string fileMappingCsvFile = "FileID_mappings_"+Environment.MachineName;
        static readonly string fileMappingCsvLocation = (string)configReader.GetValue(fileMappingCsvFile, typeof(string));

        // the assumption is that CSV file has the following structure:
        // <firstFileType>,<firstBasePath>,<firstFileId>,<secondFileType>,<secondBasePath>,<secondFileId>
        public static void InitFileMappingFromCsv(SyncConfig confInstance)
        {

            var linesRead = 0;
            if (File.Exists(fileMappingCsvLocation))
            {
                confInstance.FileMappingCsvLocation = fileMappingCsvLocation;
                var watchFileMappingFromCsv = new Stopwatch();
                watchFileMappingFromCsv.Start();
                var fileMapping = confInstance.FileMappingFromCsv;
                Console.WriteLine("\tpopulating filemapping from csv:");
                // Read data from CSV file
                try
                {
                    using (CsvFileReader reader = new CsvFileReader(fileMappingCsvLocation))
                    {
                        var dummyD = new Dictionary<string, string>();
                        CsvRow row = new CsvRow();
                        while (reader.ReadRow(row))
                        {
                            if (row.Count < 2)
                                break;
                            
                            var firstFileType = (FileType)Enum.Parse(typeof(FileType), row[0]);
                            var firstBasePath = row[1];
                            var firstFileId = row[2];
                            var firstFileExtended = confInstance.GetFileById(firstFileType, firstFileId);


                            var secondFileType = row[3];
                            var secondBasePath = row[4].Trim();
                            var secondFileId = row[5].Trim();
                            if(String.IsNullOrEmpty(secondFileType)
                                ||
                               String.IsNullOrEmpty(secondBasePath)
                               ||
                               String.IsNullOrEmpty(secondFileId)
                                )
                            {
                                confInstance.FileMapping.Add(firstFileExtended, null);
                            }
                            else
                            {
                                var secondFileType2 = (FileType)Enum.Parse(typeof(FileType), row[3]);
                                var secondFileExtended = confInstance.GetFileById(secondFileType2, secondFileId);

                                confInstance.FileMapping.Add(firstFileExtended, secondFileExtended);
                            }
                            
                            linesRead++;
                            Console.Write("\r\tlines read: " + linesRead);
                        }
                    }
                    Console.WriteLine("\n\tcompleted populating filemapping from csv");
                }
                catch (Exception ex)
                {
                    linesRead = 0;
                    fileMapping.Clear();
                    Console.WriteLine("\tcould not read the csv file: " + ex.Message);
                    Console.WriteLine("\tclearing file mapping, proceeding with populating from paths...");
                }
                finally
                {
                    watchFileMappingFromCsv.Stop();
                    Console.WriteLine("\tfile mapping lines read from csv: " + linesRead);
                    Console.WriteLine("\telapsed time: " +
                                      Init.FormatTime(watchFileMappingFromCsv.ElapsedMilliseconds));
                }
            }
            else
            {
                Console.WriteLine("CSV file was not found, proceeding with populating from paths...");
            }
            
        }

        

        public static void SaveFileMappingToCsv(SyncConfig confInstance)
        {
            var watchSaveToCsv = new Stopwatch();
            watchSaveToCsv.Start();
            Console.WriteLine("\tsaving file mapping to CSV:");
            // Write data to CSV file

            var fileMapping = confInstance.FileMapping;
            using (var writer = new CsvFileWriter(fileMappingCsvLocation))
            {
                foreach (var filePair in fileMapping)
                {
                    var file1 = filePair.Key;
                    var file2 = filePair.Value;
                    string file2FileType;
                    string file2BasePath;
                    string file2FileID;
                    if (!file2.HasValue)
                    {
                        file2FileType = "";
                        file2BasePath = "";
                        file2FileID = "";
                    }
                    else
                    {
                        file2FileType = file2.Value.fileType.ToString();
                        file2BasePath = file2.Value.basePath;
                        file2FileID = file2.Value.fileID;
                    }
                    var row = new CsvRow
                    {
                        file1.fileType.ToString(),
                        file1.basePath,
                        file1.fileID,
                        file2FileType,
                        file2BasePath,
                        file2FileID
                    };
                    writer.WriteRow(row);
                }
            }
            Console.WriteLine("\tsaving to CSV completed");
            watchSaveToCsv.Stop();
            Console.WriteLine("\telapsed time: "+Init.FormatTime(watchSaveToCsv.ElapsedMilliseconds));
        }
    }
}