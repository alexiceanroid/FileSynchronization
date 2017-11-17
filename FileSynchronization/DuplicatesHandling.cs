using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FileSynchronization
{
    public static class DuplicatesHandling
    {
        
        public static void RemoveDuplicates(SyncExecution syncExec)
        {
            
            var w = new Stopwatch();
            w.Start();
            Console.WriteLine();
            Console.WriteLine("Checking for duplicate files...");
            
            var sourceFilesList = syncExec.SourceFiles.Values.ToList();
            var destFilesList = syncExec.DestFiles.Values.ToList();

            ExcludeSomeFolders(sourceFilesList,syncExec.SyncConfig);
            ExcludeSomeFolders(destFilesList, syncExec.SyncConfig);

            sourceFilesList.Sort();
            destFilesList.Sort();

            var sourceFilesToProcess = new List<FileExtended>(sourceFilesList);
            var destFilesToProcess = new List<FileExtended>(destFilesList);

            

            var duplSourceFiles = new List<FileExtended>() {Capacity = sourceFilesToProcess.Count};
            var duplDestFiles = new List<FileExtended>() {Capacity = destFilesToProcess.Count};

            CollectDuplicateFiles(sourceFilesToProcess, duplSourceFiles);
            CollectDuplicateFiles(destFilesToProcess, duplDestFiles);

            

            if (duplSourceFiles.Count == 0 && duplDestFiles.Count == 0)
            {
                Console.WriteLine("No duplicates have been found");
            }
            else
            {
                var duplSourceValues = duplSourceFiles
                    .Select(x => x.FileNameAndSize)
                    .Distinct()
                    .ToList();
                var duplDestValues = duplDestFiles
                    .Select(x => x.FileNameAndSize)
                    .Distinct()
                    .ToList();
                int duplSourceCount = duplSourceFiles.Count - duplSourceValues.Count;
                int duplDestCount = duplDestFiles.Count - duplDestValues.Count;
                Console.WriteLine("Duplicates found: \n" + 
                                    "\tsource folders       " + duplSourceCount + "\n" +
                                    "\tdestination folders  " + duplDestCount);

                Console.WriteLine("performing cleanup of source folders...");
                CleanupDuplicateFiles(duplSourceFiles, FileType.Source, syncExec);

                Console.WriteLine("performing cleanup of destination folders...");
                CleanupDuplicateFiles(duplDestFiles, FileType.Destination, syncExec);
                Console.WriteLine("\nCleanup complete");
            }


            //WriteToLog(duplSourceFiles, duplDestFiles, logFile);

            w.Stop();
            Console.WriteLine("elapsed time: " + Init.FormatTime(w.ElapsedMilliseconds));
        }

        private static void ExcludeSomeFolders(List<FileExtended> filesList, SyncConfig syncConfig)
        {
            //var excludedFolders = new List<string>();
            XElement root = XElement.Load(syncConfig.AppConfigLocation);
            var foldersXml = root.Element("DuplicatesHandling")
                .Element("ExcludedFolders")
                .Elements("Folder");

            foreach (var folder in foldersXml)
            {
                filesList.RemoveAll(x => x.fullPath.StartsWith(folder.Value));
            }
            
        }


        private static void CleanupDuplicateFiles(List<FileExtended> duplFiles,FileType fileType,
            SyncExecution syncExec)
        {
            var currentStep = 0;
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
                    filesList.Remove(lastFile.fileID);
                    WorkingWithFiles.ArchiveFile(lastFile, syncLog, archiveFolder);
                    currentStep++;
                    Init.DisplayCompletionInfo("files processed",
                        currentStep,duplFiles.Count-duplValues.Count + currentStep);
                }
            }
            Console.Write("\rDone.                                                                ");
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
