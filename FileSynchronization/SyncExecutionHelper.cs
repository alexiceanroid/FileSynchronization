using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public partial class SyncExecution
    {
        private void MarkAsEqual(FilePairAction filePairAction)
        {
            filePairAction.actionDirection = Direction.None;
            filePairAction.actionType = ActionType.None;
            _actionList.Add(filePairAction);
        }

        private bool CreateMarkForPaths(FilePairAction filePairAction)
        {
            bool res = false;
            // Action = Create
            if (!filePairAction._file2.HasValue)
            {
                res = true;
                filePairAction.actionType = ActionType.Create;
                if (filePairAction._file1.fileType == FileType.Source)
                {
                    filePairAction.actionDirection = Direction.SourceToDestination;
                }
                else
                {
                    filePairAction.actionDirection = Direction.DestinationToSource;
                }
                _actionList.Add(filePairAction);
            }
            return res;
        }

        
        private bool UpdateMark(FilePairAction filePairAction)
        {
            bool res = false;
            var file1 = filePairAction._file1;
            var file2 = filePairAction._file2;
            // Action = Update
            DateTime file1LastUpdatedOn = DateTime.Parse(file1.lastWriteDateTime);
            DateTime file2LastUpdatedOn = DateTime.Parse(file2.Value.lastWriteDateTime);
            if (file1LastUpdatedOn.Date != file2LastUpdatedOn.Date
                || file1LastUpdatedOn.TimeOfDay != file2LastUpdatedOn.TimeOfDay)
            {
                res = true;
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
            return res;
        }
        

        private bool DeleteMark(FilePairAction filePairAction)
        {
            // Action = Delete: this works like this - for each file in mapping taken from CSV a check is made:
            //  if there is no file with the same id in source or destination files (depending on file1.fileType, corresponding list is queried)
            //      then this file must have been deleted
            bool res = false;
            var file1 = filePairAction._file1;
            var file2 = filePairAction._file2;
            var searchedFile1 = _syncConfig.GetFileById(file1.fileType, file1.fileID);
            var searchedFile2 = _syncConfig.GetFileById(file2.Value.fileType, file2.Value.fileID);
            if (searchedFile1.fileID == null && searchedFile2.fileID != null)
            {
                filePairAction.actionType = ActionType.Delete;

            }
            return res;
        }

        private bool CreateMarkForCsv(FilePairAction filePairAction)
        {
            bool res = false;

            foreach (var sourceFile in _syncConfig.SourceFiles)
            {
                bool isPresentInMapping = _syncConfig.FileMappingFromCsv.ContainsKey(sourceFile)
            }
            return res;
        }
    }
}