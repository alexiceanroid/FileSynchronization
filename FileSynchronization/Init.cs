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
            Console.WriteLine("Fetching source and destination files...");

            var folderMappings = syncExec.SyncConfig.FolderMappings;
            var watchInitFiles = new Stopwatch();
            var watchSourceFiles = new Stopwatch();
            var watchDestFiles = new Stopwatch();

            var sourceFilesTemp = new List<string>();
            var destFilesTemp = new List<string>();

            watchInitFiles.Start();

            
            foreach (var pair in folderMappings)
            {
                WorkingWithFiles.GetFiles(pair.Value.Item1, sourceFilesTemp);
                WorkingWithFiles.GetFiles(pair.Value.Item2, destFilesTemp);
            }

            _totalSourceFilesCount = sourceFilesTemp.Count;
            _totalDestFilesCount = destFilesTemp.Count;

            Console.WriteLine("Source files:      " + _totalSourceFilesCount);
            Console.WriteLine("Destination Files: " + _totalDestFilesCount);

            Console.WriteLine("Populating source files list...");
            //Task[] populatingFilesTask = new Task[2];
            //populatingFilesTask[0] = Task.Factory.StartNew(() =>
            //{
                //watchSourceFiles.Start();
                foreach (var pair in folderMappings)
                {
                    PopulateSourceFiles(syncExec, pair.Value.Item1, sourceFilesTemp);
                }
            //syncExec.SourceFiles.Sort();
            //watchSourceFiles.Stop();
            //}
            //);

            //populatingFilesTask[1] = Task.Factory.StartNew(() =>
            //    {
            //        watchDestFiles.Start();
            Console.WriteLine("\rDone.                                                                      ");
            Console.WriteLine("Populating destination files list...");
            foreach (var pair in folderMappings)
                    {
                        PopulateDestFiles(syncExec, pair.Value.Item2, destFilesTemp);
                    }
            //syncExec.DestFiles.Sort();
            //        watchDestFiles.Stop();
            //}
            //    );
            //Task.WaitAll(populatingFilesTask);

            //watchInitFiles.Stop();


            Console.WriteLine("\rDone.                                                                      ");
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

            syncExec.DestFiles = GetFilesDictionary(destinationFolder, filesList, FileType.Destination);
        }

        private static void PopulateSourceFiles(SyncExecution syncExec, string sourceFolder, List<string> filesList)
        {
            syncExec.SourceFiles = GetFilesDictionary(sourceFolder, filesList, FileType.Source);
        }

        private static Dictionary<string, FileExtended> GetFilesDictionary(string path, List<string> filesPaths, FileType fileType)
        {
            var filesInFolder = filesPaths.FindAll(x => x.Contains(path));
            int filesAdded = 0;
            int totalFilesCount = filesPaths.Count;
            FileExtended[] filesArray = new FileExtended[totalFilesCount];
            
            foreach (var filePath in filesInFolder)
            {
                var fileInfo = new FileInfo(filePath);
                var fileExtended = new FileExtended
                (
                    fileType,
                    path,
                    fileInfo.FullName,
                    Kernel32.GetCustomFileId(filePath)
                );

                filesArray[filesAdded] = fileExtended;
                filesAdded++;
                DisplayCompletionInfo("completion percentage", filesAdded, totalFilesCount);
            }

            return filesArray.ToDictionary(f => f.fileID, f => f);
        }

        

        

        

        private static void AddMissingFileMappingFromPaths(SyncExecution syncExec)
        {
            
            
            if (syncExec.SourceFiles.Count > 0)
            {
                // launch timer
                var watchAddMissingFilesToMapping = new Stopwatch();
                watchAddMissingFilesToMapping.Start();

                //var relativePathComparer = new RelativePathComparer();
                

                var sourceFilesMissingInMapping = syncExec.FilesMissingInMapping.
                    Where(x => x.fileType == FileType.Source).ToList();
                var destFilesMissingInMapping = syncExec.FilesMissingInMapping.
                    Where(x => x.fileType == FileType.Destination).ToList();

                sourceFilesMissingInMapping.Sort();
                destFilesMissingInMapping.Sort();

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
                        syncExec.FilesMissingInMapping.Count() + 2*count);
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

        public static void MapFiles(SyncExecution syncExec)
        {
            var watchFileMapping = new Stopwatch();
            Console.WriteLine("\nPreparing file mapping...");
            watchFileMapping.Start();
            Console.WriteLine("populating filemapping from csv:");
            foreach (var folderPair in syncExec.SyncConfig.FolderMappings)
            {
                CSVHelper.InitFileMappingFromCsv(syncExec,folderPair.Key);
            }

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
            //int completionPercentage =
            //    (int)Math.Round(100 * (decimal)currentStep /
            //                    someTotalCount);
            //if (completionPercentage > 100)
            //{
            //    completionPercentage = 100;
            //}
            //Console.Write("\r" + message + ": " + currentStep + ". Completion percentage: "
            //              + completionPercentage + "%");
            //draw empty progress bar
            //Console.CursorLeft = 0;
            //Console.Write("["); //start
            //Console.CursorLeft = 32;
            //Console.Write("]"); //end
            //Console.CursorLeft = 1;
            //float onechunk = 3.0f / someTotalCount;

            //draw filled part
            //int position = 1;
            //for (int i = 0; i < onechunk * currentStep; i++)
            //{
            //    Console.BackgroundColor = ConsoleColor.Green;
            //    Console.CursorLeft = position++;
            //    Console.Write(" ");
            //}

            //draw unfilled part
            //for (int i = position; i <= 31; i++)
            //{
            //    Console.BackgroundColor = ConsoleColor.Gray;
            //    Console.CursorLeft = position++;
            //    Console.Write(" ");
            //}

            //draw totals
            //Console.CursorLeft = 35;
            //Console.BackgroundColor = ConsoleColor.Black;
            //Console.Write(currentStep.ToString() + " of " + someTotalCount.ToString() + "    "); //blanks at the end remove any excess

            int barLength = 32;
            float onechunk = 1f/barLength;
            float completionRatio = (float) currentStep / someTotalCount;

            int numberOfChunksToFill = (int)Math.Floor(completionRatio / onechunk);
            int numberOfEmptyChunks = barLength - numberOfChunksToFill;

            string completedBarPart = string.Concat(Enumerable.Repeat(" ", numberOfChunksToFill));
            string emptyBarPart = string.Concat(Enumerable.Repeat(" ", numberOfEmptyChunks));

            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("\r[");

            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write(completedBarPart);

            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(emptyBarPart);

            //Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("]" + currentStep + " of " + someTotalCount);
        }
    }
}