using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;

namespace FileSynchronization
{
    
    public static class CSVHelper
    {
        
        
        // the assumption is that CSV file has the following structure:
        // <firstFileType>,<firstBasePath>,<firstFileFullPath>,<firstFileId>,
        //    <secondFileType>,<secondBasePath>,<secondFullPath>,<secondFileId>
        // public static void InitFileMappingFromCsv(SyncExecution syncExec,string folderMappingKey)
        // {
        //     string sourceBasePath = syncExec.SyncConfig.FolderMappings[folderMappingKey].Item1;
        //     string destBasePath = syncExec.SyncConfig.FolderMappings[folderMappingKey].Item2;
        //     if (syncExec.SourceFiles.Count(f => f.Value.basePath == sourceBasePath) 
        //         + syncExec.DestFiles.Count(f => f.Value.basePath == destBasePath) 
        //         == 0)
        //     {
        //         throw new Exception(folderMappingKey 
        //             + "Could not perform the mapping: source and destination files have not been loaded yet.");
        //     }
        //     string fileMappingFolder = syncExec.SyncConfig.Parameters["FileMappingFolder"];
        //     string FileMappingCsvFullPath = fileMappingFolder + @"\" + folderMappingKey + ".csv";
        //     
        //     
        //     if (File.Exists(FileMappingCsvFullPath))
        //     {
        //         var linesRead = 0;
        //         Console.WriteLine("Fetching data from file " + FileMappingCsvFullPath + "...");
        //
        //         
        //
        //         var fileMapping = syncExec.FileMappingFromCsv;
        //         
        //         // Read data from CSV file
        //         try
        //         {
        //             var totalLines = 0;
        //             using (var reader = File.OpenText(FileMappingCsvFullPath))
        //             {
        //                 while (reader.ReadLine() != null)
        //                 {
        //                     totalLines++;
        //                 }
        //             }
        //
        //             using (CsvFileReader reader = new CsvFileReader(FileMappingCsvFullPath))
        //             {
        //                 CsvRow row = new CsvRow();
        //                 int mappingsAdded = 0;
        //                 while (reader.ReadRow(row))
        //                 {
        //                     if (row.Count < 2)
        //                         break;
        //
        //
        //                     var firstFileType = (FileType)Enum.Parse(typeof(FileType), row[0]);
        //                     var firstBasePath = row[1];
        //                     var firstFileFullPath = row[2];
        //                     var firstFileId = row[3];
        //                     var firstFileExtended = syncExec.GetFileByIdOrPath(firstFileType, firstFileId, firstFileFullPath);
        //
        //                     FileExtended secondFileExtended;
        //                     var secondFileType = row[4];
        //                     var secondBasePath = row[5].Trim();
        //                     var secondFullPath = row[6].Trim();
        //                     var secondFileId = row[7].Trim();
        //                     if(String.IsNullOrEmpty(secondFileType)
        //                         ||
        //                        String.IsNullOrEmpty(secondBasePath)
        //                        ||
        //                        String.IsNullOrEmpty(secondFileId)
        //                         )
        //                     {
        //                         continue;
        //                     }
        //                     else
        //                     {
        //                         var secondFileType2 = (FileType)Enum.Parse(typeof(FileType), secondFileType);
        //                         secondFileExtended = syncExec.GetFileByIdOrPath(secondFileType2, secondFileId, secondFullPath);
        //                     }
        //
        //                     if (firstFileExtended != null)
        //                     {
        //                         if (fileMapping.ContainsKey(firstFileExtended))
        //                             continue;
        //                         fileMapping.Add(firstFileExtended, secondFileExtended);
        //                         mappingsAdded++;
        //                     }
        //                     else
        //                     {
        //                         if (secondFileExtended != null && !fileMapping.ContainsKey(secondFileExtended))
        //                         {
        //                             fileMapping.Add(secondFileExtended, null);
        //                             mappingsAdded++;
        //                         }
        //                     }
        //
        //                     if (firstFileExtended != null
        //                         ||
        //                         secondFileExtended != null)
        //                     {
        //                         // the following method allows to determine file renaming, deletion, moving
        //                         syncExec.AppendActionListWithDeleteRenameMove(firstFileExtended, secondFileExtended,
        //                             row);
        //
        //                     }
        //                     // if this mapping needs to be persisted:
        //                     else if (syncExec.SyncConfig.AreBasePathsIncluded(firstBasePath,secondBasePath))
        //                     {
        //                         syncExec.CsvMappingToPersist.Add(row);
        //                     }
        //                     linesRead++;
        //                     //int destFilesUnmappedCount =
        //                         //syncExec.FilesMissingInMapping.Count(s => s.fileType == FileType.Destination);
        //                     var completionPercentage = (int) Math.Round(100 * (double)mappingsAdded
        //                                                                 / (syncExec.DestFiles.Count));
        //                     //Console.Write("\r\tlines read: " + linesRead + "; mapping completion: "
        //                     //    + completionPercentage + "%");
        //                     Init.DisplayCompletionInfo("line read",linesRead,totalLines);
        //                 }
        //                 Console.WriteLine("\rCompleted. File mapping lines read from csv: " + linesRead);
        //             }
        //             
        //         }
        //         catch (Exception ex)
        //         {
        //             linesRead = 0;
        //             fileMapping.Clear();
        //             Console.WriteLine("could not read the csv file: " + ex.Message);
        //             Console.WriteLine("clearing file mapping, proceeding with populating from paths...");
        //         }
        //         
        //     }
        //     else
        //     {
        //         Console.WriteLine("CSV file was not found, proceeding...");
        //     }
        //     
        // }

        

        // public static void SaveFileMappingToCsv(SyncExecution syncExec)
        // {
        //     Console.WriteLine();
        //     Console.WriteLine("Saving file mapping to CSV:");
        //     // Write data to CSV file
        //
        //     var fileMapping = syncExec.FileMapping;
        //     var csvMappingToPersist = syncExec.CsvMappingToPersist;
        //     string fileMappingCsvFolder = syncExec.SyncConfig.Parameters["FileMappingFolder"];
        //     if (!Directory.Exists(fileMappingCsvFolder))
        //         Directory.CreateDirectory(fileMappingCsvFolder);
        //
        //     foreach (var folderPair in syncExec.SyncConfig.FolderMappings)
        //     {
        //         string fileMappingCsvLocation = fileMappingCsvFolder + @"\"
        //                                         + folderPair.Key + ".csv";
        //         string sourceFolder = folderPair.Value.Item1;
        //         string destFolder = folderPair.Value.Item2;
        //         List<CsvRow> csvMappingToPersistPartial = syncExec.GetFileMappingPersistentByFoldMapKey(folderPair.Key);
        //         using (var writer = new CsvFileWriter(fileMappingCsvLocation))
        //         {
        //             foreach (var csvRow in csvMappingToPersistPartial)
        //             {
        //                 writer.WriteRow(csvRow);
        //             }
        //
        //             foreach (var filePair in fileMapping)
        //             {
        //                 var file1 = filePair.Key;
        //                 var file2 = filePair.Value;
        //                 if (file1.basePath != sourceFolder && file1.basePath != destFolder)
        //                     continue;
        //                 if (file1 == null || file2 == null)
        //                     continue;
        //
        //                 var file2FileType = file2.fileType.ToString();
        //                     var file2BasePath = file2.basePath;
        //                     var file2FileID = file2.fileID;
        //                     var file2FullPath = file2.fullPath;
        //                 
        //                 var row = new CsvRow
        //                 {
        //                     file1.fileType.ToString(),
        //                     file1.basePath,
        //                     file1.fullPath,
        //                     file1.fileID,
        //                     file2FileType,
        //                     file2BasePath,
        //                     file2FullPath,
        //                     file2FileID
        //                 };
        //                 writer.WriteRow(row);
        //             }
        //         }
        //     }
        //     Console.WriteLine("\tsaving to CSV completed");
        // }
    }
}