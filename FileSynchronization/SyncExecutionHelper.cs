using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public partial class SyncExecution
    {
        private void MarkAsEqualForPaths(FilePairAction filePairAction)
        {
            filePairAction.actionDirection = Direction.None;
            filePairAction.actionType = ActionType.None;
            AddFilePairWithCheck(filePairAction);
        }

        private bool CreateMarkForPaths(FilePairAction filePairAction)
        {
            bool res = false;
            // Action = Create
            if (filePairAction._file2 == null)
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
                AddFilePairWithCheck(filePairAction);
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
            DateTime file2LastUpdatedOn = DateTime.Parse(file2.lastWriteDateTime);
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
                AddFilePairWithCheck(filePairAction);
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
            var searchedFile2 = _syncConfig.GetFileById(file2.fileType, file2.fileID);
            if (searchedFile1.fileID == null && searchedFile2.fileID != null)
            {
                filePairAction.actionType = ActionType.Delete;

            }
            return res;
        }

        

        private bool RenameMark(FilePairAction filePairAction)
        {
            bool res = false;

            var file1 = filePairAction._file1;
            var file2 = filePairAction._file2;


            return res;
        }

        private void AddFilePairWithCheck(FilePairAction filePairAction)
        {
            if (_actionList.Contains(filePairAction))
            {
                string mes = "The actions list already contains this file combination:\n" +
                             "\t source file:      " + filePairAction._file1.fullPath + "\n" +
                             "\t destination file: " + filePairAction._file2.fullPath + "\n" +
                             "\t " + filePairAction.actionType + ", " + filePairAction.actionDirection;

                throw new Exception(mes);
            }
            _actionList.Add(filePairAction);
        }
    }
}