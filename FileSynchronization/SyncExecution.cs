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
        private Dictionary<FileExtended, FileExtended?> _mappingToRemove;
        private Dictionary<FileExtended, FileExtended?> _mappingToAdd;
        private readonly SyncConfig _syncConfig;

        public SyncExecution(SyncConfig _syncConfig)
        {
            this._syncConfig = _syncConfig;
            _actionList = new List<FilePairAction>();
            _mappingToAdd = new Dictionary<FileExtended, FileExtended?>();
            _mappingToRemove = new Dictionary<FileExtended, FileExtended?>();
        }

        public void PopulateActionList()
        {
            var sourceFiles = _syncConfig.SourceFiles;
            var destFiles = _syncConfig.DestinationFiles;
            var fileMappingFromPaths = _syncConfig.FileMappingFromPaths;
            var fileMappingFromCsv = _syncConfig.FileMappingFromCsv;

            foreach (var filePair in fileMappingFromPaths)
            {
                FilePairAction filePairAction = new FilePairAction(filePair.Key, filePair.Value);
                
                bool create = CreateMarkForPaths(filePairAction);
                if (create)
                    continue;
                
                bool update = UpdateMark(filePairAction);
                if (update)
                    continue;
                
                MarkAsEqual(filePairAction);
            }

            foreach (var filePair in fileMappingFromCsv)
            {
                FilePairAction filePairAction = new FilePairAction(filePair.Key, filePair.Value);


                bool create = CreateMarkForCsv(filePairAction);
                if (create)
                    continue;
                
                bool update = UpdateMark(filePairAction);
                if (update)
                    continue;

                bool rename = RenameMark(filePairAction);
                if (rename)
                    continue;

                bool move = MoveMark(filePairAction);
                if (move)
                    continue;

                bool delete = DeleteMark(filePairAction);
                if (delete)
                    continue;


                MarkAsEqual(filePairAction);
            }
        }

        
        public void Start()
        {
            throw new NotImplementedException();
        }
    }
}