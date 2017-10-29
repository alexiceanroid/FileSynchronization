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
        private List<FilePairAction> _actionList;
        private Dictionary<FileExtended, FileExtended> _mappingToRemove;
        private Dictionary<FileExtended, FileExtended> _mappingToAdd;
        //private List<FileExtended> _sourceFilesNew;
        //private List<FileExtended> _destFilesNew;
        private readonly SyncConfig _syncConfig;

        public SyncExecution(SyncConfig _syncConfig)
        {
            this._syncConfig = _syncConfig;
            _actionList = new List<FilePairAction>();
            _mappingToAdd = new Dictionary<FileExtended, FileExtended>();
            _mappingToRemove = new Dictionary<FileExtended, FileExtended>();
            //_sourceFilesNew = new List<FileExtended>();
            //_destFilesNew = new List<FileExtended>();
        }
        #region Properties from SyncConfig
        public List<FileExtended> MappingKeys 
            => 
            _syncConfig.FileMappingFromCsv.Keys.ToList();

        public List<FileExtended> MappingValues
            =>
                _syncConfig.FileMappingFromCsv.Values.ToList();

        public Dictionary<FileExtended, FileExtended> FileMapping
            =>
                _syncConfig.FileMapping;

        public Dictionary<FileExtended, FileExtended> FileMappingFromCsv
            =>
                _syncConfig.FileMappingFromCsv;

        public Dictionary<FileExtended, FileExtended> FileMappingFromPaths
            =>
                _syncConfig.FileMappingFromPaths;

        public List<FileExtended> SourceFiles
            =>
                _syncConfig.SourceFiles;

        public List<FileExtended> DestFiles
            =>
                _syncConfig.DestinationFiles;
        #endregion

        /*
        private void PopulateNewFiles()
        {
            if (FileMappingFromCsv.Count > 0)
            {
                List<FileExtended> mappingKeys = FileMappingFromCsv.Keys.ToList();
                List<FileExtended> mappingValues = new List<FileExtended>();

                var mappingValuesCollection = FileMappingFromCsv.Values.Where(x => x != null);
                foreach (var mappingValue in mappingValuesCollection)
                {
                    mappingValues.Add(mappingValue);
                }

                _sourceFilesNew = SourceFiles.Except(mappingKeys).ToList();
                _destFilesNew = DestFiles.Except(mappingKeys).ToList();
                _destFilesNew = _destFilesNew.Except(mappingValues).ToList();
            }
        }
        */
        

        public void PopulateActionList()
        {
            //PopulateNewFiles();

            foreach (var filePair in FileMappingFromPaths)
            {
                FilePairAction filePairAction = new FilePairAction(filePair.Key, filePair.Value);
                
                
                bool create = CreateMarkForPaths(filePairAction);
                if (create)
                    continue;
                
                
                bool update = UpdateMark(filePairAction);
                if (update)
                    continue;
                
                
                MarkAsEqualForPaths(filePairAction);
            }

            foreach (var filePair in FileMappingFromCsv)
            {
                FilePairAction filePairAction = new FilePairAction(filePair.Key, filePair.Value);

                /*
                bool create = CreateMarkForCsv(filePairAction);
                if (create)
                    continue;
                */

                bool update = UpdateMark(filePairAction);
                if (update)
                    continue;
                
                bool rename = RenameMark(filePairAction);
                if (rename)
                    continue;
                /*
                
                bool move = MoveMark(filePairAction);
                if (move)
                    continue;
                    

                bool delete = DeleteMark(filePairAction);
                if (delete)
                    continue;
                   */

                MarkAsEqualForPaths(filePairAction);
            }
        }

        
        public void Start()
        {
            foreach (var action in _actionList)
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