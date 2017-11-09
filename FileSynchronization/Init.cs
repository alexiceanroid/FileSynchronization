using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace FileSynchronization
{

    public static class Init
    {
        private static int _totalSourceFilesCount = 0;
        private static int _totalDestFilesCount = 0;
        private static string _additonalMappingFromPaths = "No";
        
        private static int _fileMappingCountPaths = 0;

        public static void InitializeFiles(SyncExecution syncExec)
        {
            var folderMappings = syncExec.SyncConfig.FolderMappings;
            var watchInitFiles = new Stopwatch();
            var watchSourceFiles = new Stopwatch();
            var watchDestFiles = new Stopwatch();

            var sourceFilesTemp = new List<string>();
            var destFilesTemp = new List<string>();

            watchInitFiles.Start();

            Console.WriteLine("\n");
            Console.WriteLine("Populating source and destination files lists...");
            foreach (var pair in folderMappings)
            {
                WorkingWithFiles.GetFiles(pair.Key, sourceFilesTemp);
                WorkingWithFiles.GetFiles(pair.Value, destFilesTemp);
            }

            _totalSourceFilesCount = sourceFilesTemp.Count;
            _totalDestFilesCount = destFilesTemp.Count;

            Task[] populatingFilesTask = new Task[2];
            populatingFilesTask[0] = Task.Factory.StartNew(() =>
            {
                watchSourceFiles.Start();
                syncExec.SourceFiles.Capacity = _totalSourceFilesCount;
                foreach (var pair in folderMappings)
                {
                    PopulateSourceFiles(syncExec, pair.Key, sourceFilesTemp);
                }
                watchSourceFiles.Stop();
            }
            );

            populatingFilesTask[1] = Task.Factory.StartNew(() =>
                {
                    watchDestFiles.Start();
                    syncExec.DestFiles.Capacity = _totalDestFilesCount;
                    foreach (var pair in folderMappings)
                    {
                        PopulateDestFiles(syncExec, pair.Value, destFilesTemp);
                    }
                    watchDestFiles.Stop();
                }
            );
            Task.WaitAll(populatingFilesTask);
            watchInitFiles.Stop();

            
            Console.WriteLine("Done:");
            Console.WriteLine($"Elapsed time: {FormatTime(watchInitFiles.ElapsedMilliseconds)}");
            //Console.WriteLine($"\tprocessed {_sourceFilesProcessed} of {_totalSourceFilesCount} source files");
            //Console.WriteLine($"\telapsed time: {FormatTime(watchSourceFiles.ElapsedMilliseconds)}");
            //Console.WriteLine($"\tprocessed {_destFilesProcessed} of {_totalDestFilesCount} destination files");
            //Console.WriteLine($"\telapsed time: {FormatTime(watchDestFiles.ElapsedMilliseconds)}");

        }

        public static string FormatTime(long milliseconds)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);
            string timeString = $"{timeSpan.Hours} h {timeSpan.Minutes} min {timeSpan.Seconds} sec";
            return timeString;
        }

        private static void PopulateDestFiles(SyncExecution syncExec, string destinationFolder, List<string> filesList)
        {
            
            PopulateFileLists(destinationFolder, syncExec.DestFiles,FileType.Destination, filesList);
        }

        private static void PopulateSourceFiles(SyncExecution syncExec, string sourceFolder, List<string> filesList)
        {
            PopulateFileLists(sourceFolder, syncExec.SourceFiles,FileType.Source, filesList);
        }

        private static void PopulateFileLists(string path, List<FileExtended> fileInfos, FileType fileType, List<string> filesList)
        {
            string basePath = String.Copy(path);
            //var fileList = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);

            foreach (var file in filesList)
            {
                var filePath = String.Copy(file);
                var fileInfo = new FileInfo(filePath);
                var fileExtended = new FileExtended
                (
                    fileType,
                    basePath,
                    fileInfo.FullName,
                    fileInfo.LastWriteTime.ToString(CultureInfo.InvariantCulture),
                    Kernel32.GetCustomFileId(filePath)
                );

                fileInfos.Add(fileExtended);
            }
            
        }

        

        

        

        private static void AddMissingFileMappingFromPaths(SyncExecution syncExec)
        {
            
            
            if (syncExec.SourceFiles.Count > 0)
            {
                // launch timer
                var watchAddMissingFilesToMapping = new Stopwatch();
                watchAddMissingFilesToMapping.Start();

                var sourceFilesMissingInMapping = syncExec.FilesMissingInMapping.Where(x => x.fileType == FileType.Source);
                var destFilesMissingInMapping = syncExec.FilesMissingInMapping.Where(x => x.fileType == FileType.Destination);
                int expectedFileMappingEntriesCount = (new int[] { sourceFilesMissingInMapping.Count(),
                    destFilesMissingInMapping.Count()}).Max();
                var fileMappingFromPaths = syncExec.FileMappingFromPaths;

                var sourceFilesWithoutCounterpart = new List<FileExtended>(sourceFilesMissingInMapping);
                var destFilesWithoutCounterpart = new List<FileExtended>(destFilesMissingInMapping);

                Console.WriteLine();
                Console.WriteLine("\tStarting populating missing FileMapping from paths:");
                if (syncExec.FilesMissingInMapping.Count > 0)
                {
                    _additonalMappingFromPaths = "Yes";
                }

                // append file mapping from paths with all intersecting source and destination files combinations
                var intersectionMapping = from s in sourceFilesMissingInMapping
                                          join d in destFilesMissingInMapping
                        on s.RelativePath equals d.RelativePath
                    select new {file1 = s, file2 = d};
                
                foreach (var filePair in intersectionMapping)
                {
                    if (!fileMappingFromPaths.ContainsKey(filePair.file1))
                    {
                        fileMappingFromPaths.Add(filePair.file1, filePair.file2);
                        sourceFilesWithoutCounterpart.Remove(filePair.file1);
                        destFilesWithoutCounterpart.Remove(filePair.file2);

                        FileMappingCompletionInfo(expectedFileMappingEntriesCount);
                    }
                }

                // append the mapping with source files for which no destination match has been found
                foreach (var s in sourceFilesWithoutCounterpart)
                {
                    fileMappingFromPaths.Add(s, null);
                    
                    FileMappingCompletionInfo(expectedFileMappingEntriesCount);
                }

                // append the mapping with destination files for which no source match has been found
                foreach (var d in destFilesWithoutCounterpart)
                {
                    fileMappingFromPaths.Add(d, null);

                    FileMappingCompletionInfo(expectedFileMappingEntriesCount);
                }

                Console.Write("\r\tfinished populating missing FileMapping from paths. Added " + fileMappingFromPaths.Count + " entries.");
                Console.WriteLine("\n\telapsed time: "+FormatTime(watchAddMissingFilesToMapping.ElapsedMilliseconds));

            }
            else
            {
                throw new Exception("Source files not loaded yet");
            }
        }

        public static void InitFileMapping(SyncExecution syncExec)
        {
            var watchFileMapping = new Stopwatch();
            Console.WriteLine("\nPreparing file mapping...");
            watchFileMapping.Start();

            CSVHelper.InitFileMappingFromCsv(syncExec);

            //bool csvExists = File.Exists(syncExec.FileMappingCsvLocation);
            //DateTime csvLastWrite = DateTime.MinValue;
            //DateTime appConfLastWrite = DateTime.MinValue;
            //if (csvExists)
            //{
            //    csvLastWrite = (new FileInfo(syncExec.FileMappingCsvLocation)).LastWriteTime;
            //    appConfLastWrite = (new FileInfo(syncExec.SyncConfig.AppConfigLocation)).LastWriteTime;
            //}
            
            // append existing file mapping if app_config has been modified later than csv mapping file
            // or if csv file does not exist
            if ( syncExec.FilesMissingInMapping.Count > 0)
              
            { 
                AddMissingFileMappingFromPaths(syncExec);
            }
            watchFileMapping.Stop();
            Console.WriteLine("File mapping complete!");
            Console.WriteLine($"elapsed time: {FormatTime(watchFileMapping.ElapsedMilliseconds)}");
        }

        private static void FileMappingCompletionInfo(int expectedFileMappingEntriesCount)
        {
            _fileMappingCountPaths++;
            int completionPercentage =
                (int)Math.Round(100 * (decimal)_fileMappingCountPaths /
                                expectedFileMappingEntriesCount);
            if (completionPercentage < 100)
            {
                //completionPercentage = 100;
                Console.Write("\t\radded " + _fileMappingCountPaths +
                              " entries to file mapping. Completion percentage: "
                              + completionPercentage + "%");
            }
        }
    }
}