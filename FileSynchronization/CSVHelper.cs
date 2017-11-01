using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Text;

namespace FileSynchronization
{
    
    public static class CSVHelper
    {
        
        static readonly AppSettingsReader configReader = new AppSettingsReader();
        private static readonly string fileMappingCsvFile = "FileID_mappings_"+Environment.MachineName;
        static readonly string fileMappingCsvLocation = (string)configReader.GetValue(fileMappingCsvFile, typeof(string));

        // the assumption is that CSV file has the following structure:
        // <firstFileType>,<firstBasePath>,<firstFileFullPath>,<firstFileId>,
        //    <secondFileType>,<secondBasePath>,<secondFullPath>,<secondFileId>
        public static void InitFileMappingFromCsv(SyncExecution syncExec)
        {

            var linesRead = 0;
            if (File.Exists(fileMappingCsvLocation))
            {
                syncExec.FileMappingCsvLocation = fileMappingCsvLocation;
                var watchFileMappingFromCsv = new Stopwatch();
                watchFileMappingFromCsv.Start();
                var fileMapping = syncExec.FileMappingFromCsv;
                Console.WriteLine("\tpopulating filemapping from csv:");
                // Read data from CSV file
                try
                {
                    using (CsvFileReader reader = new CsvFileReader(fileMappingCsvLocation))
                    {
                        CsvRow row = new CsvRow();
                        while (reader.ReadRow(row))
                        {
                            if (row.Count < 2)
                                break;


                            var firstFileType = (FileType)Enum.Parse(typeof(FileType), row[0]);
                            var firstBasePath = row[1];
                            var firstFileFullPath = row[2];
                            var firstFileId = row[3];
                            var firstFileExtended = syncExec.GetFileByIdOrPath(firstFileType, firstFileId, firstFileFullPath);

                            FileExtended secondFileExtended;
                            var secondFileType = row[4];
                            var secondBasePath = row[5].Trim();
                            var secondFullPath = row[6].Trim();
                            var secondFileId = row[7].Trim();
                            if(String.IsNullOrEmpty(secondFileType)
                                ||
                               String.IsNullOrEmpty(secondBasePath)
                               ||
                               String.IsNullOrEmpty(secondFileId)
                                )
                            {
                                throw new Exception("No second file provided in CSV file mapping");
                            }
                            else
                            {
                                var secondFileType2 = (FileType)Enum.Parse(typeof(FileType), secondFileType);
                                secondFileExtended = syncExec.GetFileByIdOrPath(secondFileType2, secondFileId, secondFullPath);
                            }

                            if (firstFileExtended != null)
                            {
                                fileMapping.Add(firstFileExtended, secondFileExtended);
                            }
                            else
                            {
                                if(secondFileExtended != null)
                                    fileMapping.Add(secondFileExtended, firstFileExtended);
                            }

                            // the following method allows to determine file renaming, deletion, moving
                            syncExec.AppendActionListWithDeleteRenameMove(firstFileExtended, secondFileExtended, row);

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

        

        public static void SaveFileMappingToCsv(SyncExecution syncExec)
        {
            var watchSaveToCsv = new Stopwatch();
            watchSaveToCsv.Start();
            Console.WriteLine();
            Console.WriteLine("Saving file mapping to CSV:");
            // Write data to CSV file

            var fileMapping = syncExec.FileMapping;
            using (var writer = new CsvFileWriter(fileMappingCsvLocation))
            {
                foreach (var filePair in fileMapping)
                {
                    var file1 = filePair.Key;
                    var file2 = filePair.Value;
                    string file2FileType;
                    string file2BasePath;
                    string file2FullPath;
                    string file2FileID;
                    if (file2 == null)
                    {
                        file2FileType = "";
                        file2BasePath = "";
                        file2FileID = "";
                        file2FullPath = "";
                    }
                    else
                    {
                        file2FileType = file2.fileType.ToString();
                        file2BasePath = file2.basePath;
                        file2FileID = file2.fileID;
                        file2FullPath = file2.fullPath;
                    }
                    var row = new CsvRow
                    {
                        file1.fileType.ToString(),
                        file1.basePath,
                        file1.fullPath,
                        file1.fileID,
                        file2FileType,
                        file2BasePath,
                        file2FullPath,
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