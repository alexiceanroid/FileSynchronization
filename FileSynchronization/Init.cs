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

        private static int _fileMappingCountPaths = 0;

        public static void InitializeFiles(SyncExecution syncExec)
        {
            Console.WriteLine("\n");
            Console.WriteLine("Populating source and destination files lists...");
            var folderMappings = syncExec.SyncConfig.FolderMappings;
            var watchInitFiles = new Stopwatch();
            var watchSourceFiles = new Stopwatch();
            var watchDestFiles = new Stopwatch();

            var sourceFilesTemp = new List<string>();
            var destFilesTemp = new List<string>();

            watchInitFiles.Start();

            
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
            Console.WriteLine("Source files:      " + syncExec.SourceFiles.Count);
            Console.WriteLine("Destination files: " + syncExec.DestFiles.Count);
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

                var sourceFilesMissingInMapping = syncExec.FilesMissingInMapping.
                    Where(x => x.fileType == FileType.Source).ToList();
                var destFilesMissingInMapping = syncExec.FilesMissingInMapping.
                    Where(x => x.fileType == FileType.Destination).ToList();
                int expectedFileMappingEntriesCount = Math.Max(sourceFilesMissingInMapping.Count,
                    destFilesMissingInMapping.Count);
                var fileMappingFromPaths = syncExec.FileMappingFromPaths;

                var sourceFilesWithoutCounterpart = new List<FileExtended>
                    { Capacity = expectedFileMappingEntriesCount};

                var sourceFilesToProcess = new List<FileExtended>(sourceFilesMissingInMapping);
                var destFilesToProcess = new List<FileExtended>(destFilesMissingInMapping);

                Console.WriteLine();
                Console.WriteLine("Populating missing FileMapping from paths:");

                int count = 0;
                while (sourceFilesToProcess.Count > 0)
                {
                    count++;
                    int lastInd = sourceFilesToProcess.Count - 1;
                    FileExtended sourceFile = sourceFilesToProcess[lastInd];
                    FileExtended destMatch = null;
                    for (int i = destFilesToProcess.Count-1; i>=0; i--)
                    {
                        FileExtended destFile = destFilesToProcess[i];
                        if (destFile.RelativePath == sourceFile.RelativePath)
                        {
                            destMatch = destFile;
                            break;
                        }
                    }
                    if (destMatch != null)
                    {
                        destFilesToProcess.Remove(destMatch);
                        fileMappingFromPaths.Add(sourceFile,destMatch);
                    }
                    else
                    {
                        for (int i = destFilesToProcess.Count - 1; i >= 0; i--)
                        {
                            FileExtended destFile = destFilesToProcess[i];
                            if (destFile.FileNameAndSize == sourceFile.FileNameAndSize)
                            {
                                destMatch = destFile;
                                break;
                            }
                        }
                        if (destMatch != null)
                        {
                            destFilesToProcess.Remove(destMatch);
                            fileMappingFromPaths.Add(sourceFile, destMatch);
                            
                        }
                        else
                        {
                            sourceFilesWithoutCounterpart.Add(sourceFile);
                        }
                    }
                    sourceFilesToProcess.Remove(sourceFile);
                    DisplayCompletionInfo("files processed", count,
                        syncExec.FilesMissingInMapping.Count() + count);
                }

                foreach (var sourceFile in sourceFilesWithoutCounterpart)
                {
                    count++;
                    fileMappingFromPaths.Add(sourceFile,null);
                    DisplayCompletionInfo("files processed", count,
                        syncExec.FilesMissingInMapping.Count() + count);
                }

                while (destFilesToProcess.Count > 0)
                {
                    int destFileInd = destFilesToProcess.Count - 1;
                    var destFile = destFilesToProcess[destFileInd];
                    count++;
                    fileMappingFromPaths.Add(destFile,null);
                    destFilesToProcess.Remove(destFile);
                    DisplayCompletionInfo("files processed", count,
                        destFilesToProcess.Count + count);
                }

                Console.Write("\rfinished populating missing FileMapping from paths. Added " + fileMappingFromPaths.Count + " entries.");
                Console.WriteLine("\nelapsed time: "+FormatTime(watchAddMissingFilesToMapping.ElapsedMilliseconds));

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

            
            // append existing file mapping if app_config has been modified later than csv mapping file
            // or if csv file does not exist
            if ( syncExec.FilesMissingInMapping.Any())
              
            { 
                AddMissingFileMappingFromPaths(syncExec);
            }
            watchFileMapping.Stop();
            Console.WriteLine("File mapping complete!");
            Console.WriteLine($"elapsed time: {FormatTime(watchFileMapping.ElapsedMilliseconds)}");
        }

        public static void DisplayCompletionInfo(string message, int currentStep, int someTotalCount)
        {
            int completionPercentage =
                (int)Math.Round(100 * (decimal)currentStep /
                                someTotalCount);
            if (completionPercentage > 100)
            {
                completionPercentage = 100;
            }
            Console.Write("\r" + message + ": " + currentStep + ". Completion percentage: "
                          + completionPercentage + "%");
        }
    }
}