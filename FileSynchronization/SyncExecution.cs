using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public class SyncExecution
    {
        private List<FilePairAction> _actionList;
        private SyncConfig _syncConfig;

        public SyncExecution(SyncConfig _syncConfig)
        {
            this._syncConfig = _syncConfig;
            this._actionList = new List<FilePairAction>();
        }

        public void PopulateActionList()
        {
            var sourceFiles = _syncConfig.SourceFiles;
            var destFiles = _syncConfig.DestinationFiles;
            var fileMapping = _syncConfig.FileMapping;

            foreach (var filePair in fileMapping)
            {
                var file1 = filePair.Key;
                var file2 = filePair.Value;
                FilePairAction filePairAction = new FilePairAction(file1, file2);

                // Action = Create
                if (!file2.HasValue)
                {
                    filePairAction.actionType = ActionType.Create;
                    if (file1.fileType == FileType.Source)
                    {
                        filePairAction.actionDirection = Direction.SourceToDestination;
                    }
                    else
                    {
                        filePairAction.actionDirection = Direction.DestinationToSource;
                    }
                    _actionList.Add(filePairAction);
                    continue;
                }

                // Action = Rename
                

                // Action = Move
                // this will be available after a first snapshot of FileMapping is captured in local database

                // Action = Delete
                var searchedFile1 = _syncConfig.GetFileById(file1.fileType, file1.fileID);
                var searchedFile2 = _syncConfig.GetFileById(file2.Value.fileType, file2.Value.fileID);
                //if(searchedFile1.fileID != null && searchedFile2.fileID == null)

                


                // Action = Update
                DateTime file1LastUpdatedOn = DateTime.Parse(file1.lastWriteDateTime);
                DateTime file2LastUpdatedOn = DateTime.Parse(file2.Value.lastWriteDateTime);
                if (file1LastUpdatedOn.Date != file2LastUpdatedOn.Date
                    && file1LastUpdatedOn.TimeOfDay != file2LastUpdatedOn.TimeOfDay)
                {
                    filePairAction.actionType = ActionType.Update;
                    if (file1LastUpdatedOn > file2LastUpdatedOn)
                    {
                        if (file1.fileType == FileType.Source)
                        {
                            filePairAction.actionDirection = Direction.SourceToDestination;
                        }
                        else
                        {
                            filePairAction.actionDirection = Direction.DestinationToSource;
                        }
                    }
                    else
                    {
                        if (file1.fileType == FileType.Source)
                        {
                            filePairAction.actionDirection = Direction.DestinationToSource;
                        }
                        else
                        {
                            filePairAction.actionDirection = Direction.SourceToDestination;
                        }
                    }
                    _actionList.Add(filePairAction);
                }

                
            }
        }
    }
}