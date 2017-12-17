using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public partial class SyncExecution
    {
        
        private List<FilePairAction> _actionList;
        private List<FilePairAction> _failedActions;
        private int _filesCreated;
        private int _filesUpdated;
        private int _filesRenamedMoved;
        private int _filesRenamed;
        private int _filesMoved;
        private int _filesDeleted;
        private int _spaceNeededInSource;
        private int _spaceNeededInDestination;

        public readonly SyncConfig SyncConfig;
        //public string FileMappingCsvLocation;
        public List<FilePairAction> FailedActions => _failedActions;

        public Dictionary<string,FileExtended> SourceFiles { get; set; }
        public Dictionary<string,FileExtended> DestFiles { get; set; }

        // this is mapping from existing csv file for folder mapping
        //  that is no longer found in config file. This, however, can be needed
        //  in future, in case the folder mapping gets re-enabled in the config:
        public List<CsvRow> CsvMappingToPersist { get; set; } 

        public Dictionary<FileExtended, FileExtended> FileMappingFromCsv { get; set; }
        public Dictionary<FileExtended, FileExtended> FileMappingFromPaths { get; set; }

        public int SpaceNeededInSource => _spaceNeededInSource;
        public int SpaceNeededInDestination => _spaceNeededInDestination;

        private void CalculateSpaceNeeded()
        {
            long sourceDeletionSize = 0; // B
            long sourceCreationSize = 0;
            long destDeletionSize = 0;
            long destCreationSize = 0;

            var creationsAndDeletions =
                ActionsList.FindAll(x => x.ActionType == ActionType.Create || x.ActionType == ActionType.Delete);
            foreach (var action in creationsAndDeletions)
            {
                var files = WorkingWithFiles.GetSourceAndDestFile(action.File1, action.File2);
                var sourceFile = files[FileType.Source];
                var destFile = files[FileType.Destination];

                if (action.ActionType == ActionType.Create)
                {
                    if (action.ActionDirection == Direction.SourceToDestination)
                        destCreationSize += sourceFile.FileSize;
                    else
                    {
                        sourceCreationSize += destFile.FileSize;
                    }
                }

                if (action.ActionType == ActionType.Delete)
                {
                    if (action.ActionDirection == Direction.SourceToDestination)
                    {
                        destDeletionSize += destFile.FileSize;
                    }
                    else
                    {
                        sourceDeletionSize += sourceFile.FileSize;
                    }
                }
            }

            _spaceNeededInSource = (int) Math.Round( (double)(sourceCreationSize - sourceDeletionSize) / (1024*1024));
            _spaceNeededInDestination = (int)Math.Round((double)(destCreationSize - destDeletionSize) / (1024 * 1024));
        }

        


        

        public SyncExecution(SyncConfig SyncConfig)
        {
            this.SyncConfig = SyncConfig;
            
            _actionList = new List<FilePairAction>();
            //SourceFiles = new Dictionary<string, FileExtended>();
            //DestFiles = new Dictionary<string, FileExtended>();
            FileMappingFromCsv = new Dictionary<FileExtended, FileExtended>();
            FileMappingFromPaths = new Dictionary<FileExtended, FileExtended>();
            _failedActions = new List<FilePairAction>();
            CsvMappingToPersist = new List<CsvRow>();
            _spaceNeededInSource = 0;
            _spaceNeededInDestination = 0;
        }

        public bool AnyChangesNeeded
        {
            get
            {
                int numOfChanges = _actionList.FindAll(x => x.ActionType != ActionType.None).Count;
                return numOfChanges > 0;
            }
        }

        public List<FilePairAction> ActionsList => _actionList;

        public Dictionary<FileExtended, FileExtended> FileMapping =>
            //(Dictionary<FileExtended, FileExtended>) 
            FileMappingFromCsv.Union(FileMappingFromPaths).ToDictionary(s => s.Key, s => s.Value);

        public bool FilesMatchBasedOnPaths(FileExtended f1, FileExtended f2)
        {
            var filesMatch = false;
            var basePathsCompatible = false;
            foreach (var folderMapping in SyncConfig.FolderMappings)
            {
                var f1BasePathMatchesKey = f1.basePath == folderMapping.Value.Item1;
                var f1BasePathMatchesValue = f1.basePath == folderMapping.Value.Item2;
                var f2BasePathMatchesKey = f2.basePath == folderMapping.Value.Item1;
                var f2BasePathMatchesValue = f2.basePath == folderMapping.Value.Item2;

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
            Dictionary<string,FileExtended> filesListToSearch;
            filesListToSearch = f.fileType == FileType.Source ? DestFiles : SourceFiles;

            foreach (var candidateFile in filesListToSearch)
            {
                if (FilesMatchBasedOnPaths(f, candidateFile.Value))
                {
                    resultingFile = candidateFile.Value;
                    break;
                }
            }

            return resultingFile;
        }

        public FileExtended GetFileById(FileType fileType, string id)
        {
            FileExtended resultingFile = null;
            Dictionary<string,FileExtended> listToSearch;
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
            if (listToSearch.ContainsKey(id))
            {
                resultingFile = listToSearch[id];
            }

            return resultingFile;
        }

        public FileExtended GetFileByFullPath(string fullPath)
        {
            foreach (var file in SourceFiles.Values)
            {
                if (file.fullPath == fullPath)
                {
                    return file;
                }
            }

            foreach (var file in DestFiles.Values)
            {
                if (file.fullPath == fullPath)
                {
                    return file;
                }
            }

            return null;
        }

        //public bool FileMappingMissingFiles()
        //{
        //    return FilesMissingInMapping.Count > 0;
        //}

        public IEnumerable<FileExtended> FilesMissingInMapping => FilesFromLists.Except(FilesFromMapping);

        public IEnumerable<FileExtended> FilesFromLists => SourceFiles.Values.Union(DestFiles.Values);

        public IEnumerable<FileExtended> FilesFromMapping => FileMapping.Keys.Union(FileMapping.Values);

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
        


        public void AppendActionListWithUpdateCreateMove()
        {
            Console.WriteLine("\n");
            Console.WriteLine("Calculating necessary actions to perform...");
            int count = 0;
            foreach (var filePair in FileMappingFromPaths)
            {
                count++;
                try
                {
                    bool create = AddCreateAction(filePair);
                    if (create)
                    {
                        continue;
                    }
                
                
                    AddMoveAction(filePair);
                    AddUpdateAction(filePair);
                }
                catch (Exception e)
                {
                    // construct AppError
                    string entry = "trying to identify action. File1: " + filePair.Key.fullPath
                                   + "; File2: " + filePair.Value.fullPath;
                    string method = MethodBase.GetCurrentMethod().ToString();
                    var error = new AppError(DateTime.Now, method, entry, e.Message);
                    ErrorHandling.WriteErrorLog(SyncConfig, error);
                }


                //Init.DisplayCompletionInfo("entries processed from file mapping", count,FileMapping.Count);
            }

            foreach (var filePair in FileMappingFromCsv)
            {
                count++;
                try
                {
                    AddUpdateAction(filePair);
                }
                catch (Exception e)
                {
                    // construct AppError
                    string entry = "trying to identify action. File1: " + filePair.Key.fullPath
                                   + "; File2: " + filePair.Value.fullPath;
                    string method = MethodBase.GetCurrentMethod().ToString();
                    var error = new AppError(DateTime.Now, method, entry, e.Message);
                    ErrorHandling.WriteErrorLog(SyncConfig, error);
                }
                //Init.DisplayCompletionInfo("entries processed from file mapping", count, FileMapping.Count);
            }
            _actionList.Sort();
            CalculateSpaceNeeded();
            Console.WriteLine("Done.");
        }


        public void PerformActions()
        {
            Console.WriteLine();
            var actionsToPerform = _actionList.FindAll(x => x.ActionType != ActionType.None);
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
                var filesDict = WorkingWithFiles.GetSourceAndDestFile(action.File1, action.File2);
                FileExtended sourceFile = filesDict[FileType.Source];
                FileExtended destFile = filesDict[FileType.Destination];

                
                try
                {
                    switch (action.ActionType)
                    {

                        case ActionType.Create:
                            var creatingFileOp = action.ActionDirection == Direction.SourceToDestination
                                ? sourceFile.fullPath
                                : destFile.fullPath;
                            DisplaySyncProcessStats("Copying file " + creatingFileOp);
                            ActionCreate(sourceFile, destFile, action.ActionDirection);
                            _filesCreated++;
                            
                            //Init.DisplayCompletionInfo("actions performed",
                            //    _filesCreated + _filesDeleted + _filesMoved + _filesRenamed + _filesRenamedMoved + _filesUpdated,
                            //    ActionsList.Count);
                            break;

                        case ActionType.Update:
                            var updatingFile = action.ActionDirection == Direction.SourceToDestination
                                ? destFile.fullPath
                                : sourceFile.fullPath;
                            DisplaySyncProcessStats("Updating file " + updatingFile);
                            ActionUpdate(sourceFile, destFile, action.ActionDirection);
                            _filesUpdated++;
                            
                            break;

                        case ActionType.RenameMove:
                            var movingFile = action.ActionDirection == Direction.SourceToDestination
                                ? destFile.fullPath
                                : sourceFile.fullPath;
                            DisplaySyncProcessStats("Renaming and moving file " + movingFile);
                            ActionRenameMove(sourceFile, destFile, action.ActionDirection);
                            _filesRenamedMoved++;
                            
                            break;

                        case ActionType.Rename:
                            var renamingFile = action.ActionDirection == Direction.SourceToDestination
                                ? destFile.fullPath
                                : sourceFile.fullPath;
                            DisplaySyncProcessStats("Renaming file " + renamingFile);
                            ActionRenameMove(sourceFile, destFile, action.ActionDirection);
                            _filesRenamed++;
                            
                            break;

                        case ActionType.Move:
                            var movingFile2 = action.ActionDirection == Direction.SourceToDestination
                                ? destFile.fullPath
                                : sourceFile.fullPath;
                            DisplaySyncProcessStats("Renaming and moving file " + movingFile2);
                            ActionRenameMove(sourceFile, destFile, action.ActionDirection);
                            _filesMoved++;
                            
                            break;

                        case ActionType.Delete:
                            var archivingFile = action.ActionDirection == Direction.SourceToDestination
                                ? destFile.fullPath
                                : sourceFile.fullPath;
                            DisplaySyncProcessStats("Archiving file " + archivingFile);
                            ActionDelete(sourceFile, destFile, action.ActionDirection);
                            _filesDeleted++;
                            
                            break;
                    }
                    action.SyncSuccess = true;
                }
                catch (Exception ex)
                {
                    action.SyncSuccess = false;
                    action.ExceptionMessage = ex.Message;
                    _failedActions.Add(action);
                }
            }
            syncWatch.Stop();

            Console.WriteLine("\n\nSynchronization complete! Elapsed time: " 
                + Init.FormatTime(syncWatch.ElapsedMilliseconds));
        }


        public List<CsvRow> GetFileMappingPersistentByFoldMapKey(string folderMappingKey)
        {
            string sourceFolder = SyncConfig.FolderMappings[folderMappingKey].Item1;
            string destFolder = SyncConfig.FolderMappings[folderMappingKey].Item2;
            return CsvMappingToPersist.FindAll(m =>
            {
                string folder1 = m[1];
                string folder2 = m[5];
                if (
                    (folder1 == sourceFolder && folder2 == destFolder)
                    ||
                    (folder2 == sourceFolder && folder1 == destFolder)
                )
                    return true;
                return false;
            });
        }

        
    }
}