using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public static class DuplicatesHandling
    {
        public static void RemoveDuplicates(SyncExecution syncExec)
        {
            var w = new Stopwatch();
            w.Start();
            Console.WriteLine();
            Console.WriteLine("Removing duplicates...");
            
            syncExec.SourceFiles.Sort();
            syncExec.DestFiles.Sort();

            var sourceFilesToProcess = new List<FileExtended>(syncExec.SourceFiles);
            var destFilesToProcess = new List<FileExtended>(syncExec.DestFiles);

            

            var duplSourceFiles = new List<FileExtended>() {Capacity = sourceFilesToProcess.Count};
            var duplDestFiles = new List<FileExtended>() {Capacity = destFilesToProcess.Count};

            CollectDuplicateFiles(sourceFilesToProcess, duplSourceFiles);
            CollectDuplicateFiles(destFilesToProcess, duplDestFiles);


            CleanupDuplicateFiles(duplSourceFiles, FileType.Source, syncExec);
            CleanupDuplicateFiles(duplDestFiles, FileType.Destination, syncExec);


            
            //WriteToLog(duplSourceFiles, duplDestFiles, logFile);

            w.Stop();
            Console.WriteLine("Done.");
            Console.WriteLine("elapsed time: " + Init.FormatTime(w.ElapsedMilliseconds));
        }

        private static void CleanupDuplicateFiles(List<FileExtended> duplFiles,FileType fileType,
            SyncExecution syncExec)
        {
            string syncLog = syncExec.SyncConfig.SyncLog;
            string archiveFolder = syncExec.SyncConfig.Parameters["ArchiveFolder"];
            var filesList = fileType == FileType.Source ? syncExec.SourceFiles : syncExec.DestFiles;

            var duplValues = duplFiles
                .Select(x => x.FileNameAndSize)
                .Distinct()
                .ToList();

            foreach (var v in duplValues)
            {
                var tempFiles = duplFiles.FindAll(x => x.FileNameAndSize == v);
                while (tempFiles.Count > 1)
                {
                    int lastInd = tempFiles.Count - 1;
                    var lastFile = tempFiles[lastInd];

                    tempFiles.Remove(lastFile);
                    duplFiles.Remove(lastFile);
                    filesList.Remove(lastFile);
                    WorkingWithFiles.ArchiveFile(lastFile, syncLog, archiveFolder);
                }
            }
        }

        


        private static void CollectDuplicateFiles(List<FileExtended> filesToProcess, List<FileExtended> duplFiles)
        {
            bool isDuplicate;
            while (filesToProcess.Count > 0)
            {
                var outerIndex = filesToProcess.Count - 1;
                var currentFile = filesToProcess[outerIndex];

                isDuplicate = filesToProcess.Count ==1 ? false :
                 filesToProcess[outerIndex - 1].FileNameAndSize == currentFile.FileNameAndSize;
                if (isDuplicate)
                {
                    bool exitFlag = false;
                    int innerIndex = outerIndex;
                    var duplList = new List<FileExtended> {currentFile};
                    filesToProcess.Remove(currentFile);
                    while (!exitFlag)
                    {
                        innerIndex--;
                        if (innerIndex >= 0)
                        {
                            var innerFile = filesToProcess[innerIndex];
                            if (innerFile.FileNameAndSize == currentFile.FileNameAndSize)
                            {
                                duplList.Add(innerFile);
                                filesToProcess.Remove(innerFile);
                            }
                            else
                            {
                                exitFlag = true;
                            }
                        }
                        else
                        {
                            exitFlag = true;
                        }
                    }
                    duplFiles.AddRange(duplList);
                }
                else
                {
                    filesToProcess.Remove(currentFile);
                }
            }
        }

        
    }
}
