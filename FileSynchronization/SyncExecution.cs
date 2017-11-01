using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public partial class SyncExecution
    {
        public string FileMappingCsvLocation;
        private List<FilePairAction> _actionList;
        public readonly SyncConfig SyncConfig;

        public List<FileExtended> SourceFiles { get; set; }
        public List<FileExtended> DestFiles { get; set; }
        public Dictionary<FileExtended, FileExtended> FileMappingFromCsv { get; set; }
        public Dictionary<FileExtended, FileExtended> FileMappingFromPaths { get; set; }

        public SyncExecution(SyncConfig SyncConfig)
        {
            this.SyncConfig = SyncConfig;
            _actionList = new List<FilePairAction>();
            SourceFiles = new List<FileExtended>();
            DestFiles = new List<FileExtended>();
            FileMappingFromCsv = new Dictionary<FileExtended, FileExtended>();
            FileMappingFromPaths = new Dictionary<FileExtended, FileExtended>();
        }

        public bool AnyChangesNeeded
        {
            get
            {
                int numOfChanges = _actionList.FindAll(x => x.actionType != ActionType.None).Count;
                return numOfChanges > 0;
            }
        }

        internal void DisplayActionsList()
        {
            string direction;
            string source;
            string dest;


            Console.WriteLine("\nList of actions to perform: ");
            foreach (var action in _actionList)
            {
                switch (action.actionDirection)
                {
                    case Direction.None:
                        direction = "==";
                        break;
                    case Direction.SourceToDestination:
                        direction = "=>";
                        break;
                    case Direction.DestinationToSource:
                        direction = "<=";
                        break;
                    case Direction.Unknown:
                        direction = "??";
                        break;
                    default:
                        throw new Exception("Ivalid direction");
                }

                var filesDict = GetSourceAndDestFile(action);
                source = filesDict[FileType.Source];
                dest = filesDict[FileType.Destination];

                Console.WriteLine(source + " " + 
                    direction + " " + action.actionType + " " + direction + " " +
                    dest);
            }
        }

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
            foreach (var action in _actionList.FindAll(x => x.actionType != ActionType.None))
            {
                var filesDict = GetSourceAndDestFile(action);
                string sourceFile = filesDict[FileType.Source];
                string destFile = filesDict[FileType.Destination];

                switch (action.actionType)
                {

                    case ActionType.Create:
                        ActionCreate(sourceFile,destFile,action.actionDirection);
                        break;
                    
                case ActionType.Update:
                    ActionUpdate(sourceFile, destFile, action.actionDirection);
                    break;

                case ActionType.RenameMove:
                    ActionRenameMove(sourceFile, destFile, action.actionDirection);
                    break;

                case ActionType.Rename:
                    ActionRenameMove(sourceFile, destFile, action.actionDirection);
                    break;

                case ActionType.Move:
                    ActionRenameMove(sourceFile, destFile, action.actionDirection);
                    break;

                        /*
                    case ActionType.None:
                        UpdateFileMapping(action);
                        break;

                    default:
                        throw new Exception("Invalid file pair action: " + action.actionType);
                        */
                }
            }
        }

        
    }
}