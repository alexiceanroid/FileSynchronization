﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public partial class SyncExecution
    {
        public string FileMappingCsvLocation;
        private List<FilePairAction> actionList;
        private List<FilePairAction> failedActions;
        public readonly SyncConfig SyncConfig;

        private int filesCreated { get; set; }
        private int filesUpdated { get; set; }
        private int filesRenamedMoved { get; set; }
        private int filesRenamed { get; set; }
        private int filesMoved { get; set; }
        private int filesDeleted { get; set; }


        public List<FilePairAction> FailedActions => failedActions;

        public List<FileExtended> SourceFiles { get; set; }
        public List<FileExtended> DestFiles { get; set; }
        public Dictionary<FileExtended, FileExtended> FileMappingFromCsv { get; set; }
        public Dictionary<FileExtended, FileExtended> FileMappingFromPaths { get; set; }

        public SyncExecution(SyncConfig SyncConfig)
        {
            this.SyncConfig = SyncConfig;
            actionList = new List<FilePairAction>();
            SourceFiles = new List<FileExtended>();
            DestFiles = new List<FileExtended>();
            FileMappingFromCsv = new Dictionary<FileExtended, FileExtended>();
            FileMappingFromPaths = new Dictionary<FileExtended, FileExtended>();
            failedActions = new List<FilePairAction>();
        }

        public bool AnyChangesNeeded
        {
            get
            {
                int numOfChanges = actionList.FindAll(x => x.ActionType != ActionType.None).Count;
                return numOfChanges > 0;
            }
        }

        public List<FilePairAction> ActionsList => actionList;

        public Dictionary<FileExtended, FileExtended> FileMapping =>
            //(Dictionary<FileExtended, FileExtended>) 
            FileMappingFromCsv.Union(FileMappingFromPaths).ToDictionary(s => s.Key, s => s.Value);

        public bool FilesMatchBasedOnPaths(FileExtended f1, FileExtended f2)
        {
            var filesMatch = false;
            var basePathsCompatible = false;
            foreach (var folderMapping in SyncConfig.FolderMappings)
            {
                var f1BasePathMatchesKey = f1.basePath == folderMapping.Key;
                var f1BasePathMatchesValue = f1.basePath == folderMapping.Value;
                var f2BasePathMatchesKey = f2.basePath == folderMapping.Key;
                var f2BasePathMatchesValue = f2.basePath == folderMapping.Value;

                if (
                        (f1BasePathMatchesKey && f2BasePathMatchesValue)
                        ||
                        (f1BasePathMatchesValue && f2BasePathMatchesKey)
                    )
                {
                    basePathsCompatible = true;
                    break;
                }
            }

            if (basePathsCompatible &&
                f1.RelativePath == f2.RelativePath)
            {
                filesMatch = true;
            }

            return filesMatch;
        }

        public FileExtended GetFileCounterpart(FileExtended f)
        {
            FileExtended resultingFile = null;
            List<FileExtended> filesListToSearch;
            filesListToSearch = f.fileType == FileType.Source ? DestFiles : SourceFiles;

            foreach (var candidateFile in filesListToSearch)
            {
                if (FilesMatchBasedOnPaths(f, candidateFile))
                {
                    resultingFile = candidateFile;
                    break;
                }
            }

            return resultingFile;
        }

        public FileExtended GetFileById(FileType fileType, string id)
        {
            List<FileExtended> listToSearch;
            switch (fileType)
            {
                case FileType.Source:
                    listToSearch = SourceFiles;
                    break;
                case FileType.Destination:
                    listToSearch = DestFiles;
                    break;
                default:
                    throw new Exception("Unknown file type!");
            }

            foreach (var file in listToSearch)
            {
                if (file.fileID == id)
                {
                    return file;
                }
            }

            return null;
        }

        public FileExtended GetFileByFullPath(string fullPath)
        {
            foreach (var file in SourceFiles)
            {
                if (file.fullPath == fullPath)
                {
                    return file;
                }
            }

            foreach (var file in DestFiles)
            {
                if (file.fullPath == fullPath)
                {
                    return file;
                }
            }

            return null;
        }

        public bool FileMappingMissingFiles()
        {
            return FilesMissingInMapping.Count > 0;
        }

        public List<FileExtended> FilesMissingInMapping
        {

            get
            {
                List<FileExtended> filesFromLists = SourceFiles.Union(DestFiles).ToList();
                List<FileExtended> filesFromMapping = FileMapping.Keys.Union(FileMapping.Values).ToList();

                return filesFromLists.Except(filesFromMapping).ToList();
            }
        }

        

        public FileExtended GetFileByIdOrPath(FileType fileType, string fileId, string fullPath)
        {
            FileExtended resultingFile;
            var fileFromId = GetFileById(fileType, fileId);
            //resultingFile = fileFromId ?? GetFileByFullPath(fullPath);
            if (fileFromId == null)
            {
                resultingFile = GetFileByFullPath(fullPath);
                if(resultingFile != null)
                    resultingFile.fileID = Kernel32.GetCustomFileId(fullPath);
            }
            else
            {
                resultingFile = fileFromId;
            }

            return resultingFile;
        }
        


        public void AppendActionList()
        {
            Console.WriteLine("\n");
            Console.WriteLine("Calculating necessary actions to perform...");
            foreach (var filePair in FileMappingFromPaths)
            {
                if (ActionListContainsFilePair(filePair))
                    continue;

                FilePairAction filePairAction = new FilePairAction(filePair.Key, filePair.Value);
                
                
                bool create = CreateMarkForPaths(filePairAction);
                if (create)
                {
                    AddFilePairWithCheck(filePairAction);
                    continue;
                }
                
                
                bool update = UpdateMark(filePairAction);
                if (update)
                {
                    AddFilePairWithCheck(filePairAction);
                    continue;
                }

                AddFilePairWithCheck(filePairAction);
            }

            foreach (var filePair in FileMappingFromCsv)
            {
                if (ActionListContainsFilePair(filePair))
                    continue;

                FilePairAction filePairAction = new FilePairAction(filePair.Key, filePair.Value);


                bool update = UpdateMark(filePairAction);
                if (update)
                {
                    AddFilePairWithCheck(filePairAction);
                    continue;
                }


                AddFilePairWithCheck(filePairAction);
            }
        }


        public void PerformActions()
        {
            Console.WriteLine();
            var actionsToPerform = actionList.FindAll(x => x.ActionType != ActionType.None);
            if (actionsToPerform.Count == 0)
            {
                Console.WriteLine("No changes have been detected - no actions needed");
                return;
            }

            var syncWatch = new Stopwatch();
            syncWatch.Start();

            Console.WriteLine("Starting synchronization....");
            foreach (var action in actionsToPerform)
            {
                var filesDict = GetSourceAndDestFile(action.File1, action.File2);
                FileExtended sourceFile = filesDict[FileType.Source];
                FileExtended destFile = filesDict[FileType.Destination];

                try
                {
                    switch (action.ActionType)
                    {

                        case ActionType.Create:
                            ActionCreate(sourceFile, destFile, action.ActionDirection);
                            filesCreated++;
                            DisplaySyncProcessStats();
                            break;

                        case ActionType.Update:
                            ActionUpdate(sourceFile, destFile, action.ActionDirection);
                            filesUpdated++;
                            DisplaySyncProcessStats();
                            break;

                        case ActionType.RenameMove:
                            ActionRenameMove(sourceFile, destFile, action.ActionDirection);
                            filesRenamedMoved++;
                            DisplaySyncProcessStats();
                            break;

                        case ActionType.Rename:
                            ActionRenameMove(sourceFile, destFile, action.ActionDirection);
                            filesRenamed++;
                            DisplaySyncProcessStats();
                            break;

                        case ActionType.Move:
                            ActionRenameMove(sourceFile, destFile, action.ActionDirection);
                            filesMoved++;
                            DisplaySyncProcessStats();
                            break;

                        case ActionType.Delete:
                            ActionDelete(sourceFile, destFile, action.ActionDirection);
                            filesDeleted++;
                            DisplaySyncProcessStats();
                            break;

                        /*
                    case ActionType.None:
                        UpdateFileMapping(action);
                        break;

                    default:
                        throw new Exception("Invalid file pair action: " + action.ActionType);
                        */
                    }
                    action.SyncSuccess = true;
                }
                catch (Exception ex)
                {
                    action.SyncSuccess = false;
                    action.ExceptionMessage = ex.Message;
                    failedActions.Add(action);
                }
            }
            syncWatch.Stop();

            Console.WriteLine("\nSynchronization complete! Elapsed time: " 
                + Init.FormatTime(syncWatch.ElapsedMilliseconds));
        }

        
    }
}