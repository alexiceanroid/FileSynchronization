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
                filePairAction.actionDirection = filePairAction._file1.fileType == FileType.Source ? 
                    Direction.SourceToDestination : Direction.DestinationToSource;
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
                    filePairAction.actionDirection = file1.fileType == FileType.Source ? Direction.SourceToDestination : Direction.DestinationToSource;
                }
                else
                {
                    filePairAction.actionDirection = file1.fileType == FileType.Source ? Direction.DestinationToSource : Direction.SourceToDestination;
                }
                
            }
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

        public void AppendActionListWithDeleteRenameMove(FileExtended firstFileExtended, FileExtended secondFileExtended, 
            CsvRow row)
        {
            
            var filePairAction = new FilePairAction(firstFileExtended, secondFileExtended);

            

            FileExtended sourceFile =
                firstFileExtended.fileType == FileType.Source ? firstFileExtended : secondFileExtended;
            FileExtended destFile =
                secondFileExtended.fileType == FileType.Destination ? 
                    secondFileExtended : firstFileExtended;

            // handle deletions
            bool deletion = IdentifyDeletion(sourceFile, destFile, filePairAction);
            if (deletion)
            {
                _actionList.Add(filePairAction);
                return;
            }

            // handle renaming and moving
            var oldFirstFileType = (FileType)Enum.Parse(typeof(FileType), row[0]);
            var oldFirstBasePath = row[1];
            var oldFirstFileFullPath = row[2];
            var oldFirstFileId = row[3];
            var oldFirstFile = new FileExtended(oldFirstFileType, oldFirstBasePath, oldFirstFileFullPath, oldFirstFileId);

            var oldSecondFileType = (FileType)Enum.Parse(typeof(FileType), row[4]);
            var oldSecondBasePath = row[5];
            var oldSecondFileFullPath = row[6];
            var oldSecondFileId = row[7];
            var oldSecondFile = new FileExtended(oldSecondFileType, oldSecondBasePath, oldSecondFileFullPath,
                oldSecondFileId);

            var oldSourceFile = oldFirstFileType == FileType.Source ? oldFirstFile : oldSecondFile;
            var oldDestFile = oldSecondFileType == FileType.Destination ? oldSecondFile : oldFirstFile;


            IdentifyRenameMove(sourceFile, oldSourceFile, destFile, oldDestFile, filePairAction);
            if(filePairAction.actionType == ActionType.Rename
                ||
               filePairAction.actionType == ActionType.Move
                ||
               filePairAction.actionType == ActionType.Delete)
            { _actionList.Add(filePairAction);}
        }

        private void IdentifyRenameMove(FileExtended sourceFile, FileExtended oldSourceFile,
            FileExtended destFile, FileExtended oldDestFile,
            FilePairAction filePairAction)
        {
            string sourceName = Path.GetFileName(sourceFile.fullPath);
            string sourceDirectory = Path.GetDirectoryName(sourceFile.fullPath);
            string oldSourceName = Path.GetFileName(oldSourceFile.fullPath);
            string oldSourceDirectory = Path.GetDirectoryName(oldSourceFile.fullPath);

            string destName = Path.GetFileName(destFile.fullPath);
            string destDirectory = Path.GetDirectoryName(destFile.fullPath);
            string oldDestName = Path.GetFileName(oldDestFile.fullPath);
            string oldDestDirectory = Path.GetDirectoryName(oldDestFile.fullPath);

            // identify RenameMove
            if (
                    (sourceName != oldSourceName || destName != oldDestName)
                    &&
                    (sourceDirectory != oldSourceDirectory || destDirectory != oldDestDirectory)
                )

            {
                filePairAction.actionType = ActionType.RenameMove;

                if (sourceFile.fullPath != oldSourceFile.fullPath
                    &&
                    destFile.fullPath == oldDestFile.fullPath)
                {
                    filePairAction.actionDirection = Direction.SourceToDestination;
                }
                else if (sourceFile.fullPath == oldSourceFile.fullPath
                         &&
                         destFile.fullPath != oldDestFile.fullPath)
                {
                    filePairAction.actionDirection = Direction.DestinationToSource;
                }
                else if (sourceFile.fullPath != oldSourceFile.fullPath
                         &&
                         destFile.fullPath != oldDestFile.fullPath)
                {
                    filePairAction.actionDirection = Direction.Unknown;
                }

                return;
            }

            // identify Rename
            if (sourceName != oldSourceName
                || destName != oldDestName)
            {
                filePairAction.actionType = ActionType.Rename;

                if (sourceName != oldSourceName
                    && destName == oldDestName)
                {
                    filePairAction.actionDirection = Direction.SourceToDestination;
                }
                else if (sourceName == oldSourceName
                         && destName != oldDestName)
                {
                    filePairAction.actionDirection = Direction.DestinationToSource;
                }
                else if (sourceName != oldSourceName
                         && destName != oldDestName)
                {
                    filePairAction.actionDirection = Direction.Unknown;
                }
                return;
            }

            // identify Move
            if (sourceDirectory != oldSourceDirectory
                || destDirectory != oldDestDirectory)
            {
                filePairAction.actionType = ActionType.Move;

                if (sourceDirectory != oldSourceDirectory
                    && destDirectory == oldDestDirectory)
                {
                    filePairAction.actionDirection = Direction.SourceToDestination;
                }
                else if (sourceDirectory == oldSourceDirectory
                         && destDirectory != oldDestDirectory)
                {
                    filePairAction.actionDirection = Direction.DestinationToSource;
                }
                else if (sourceDirectory != oldSourceDirectory
                         && destDirectory != oldDestDirectory)
                {
                    filePairAction.actionDirection = Direction.Unknown;
                }
            }
        }

        private bool IdentifyDeletion(FileExtended sourceFile, FileExtended destFile, 
            FilePairAction filePairAction)
        {
            bool res = false;

            if (sourceFile == null && destFile != null)
            {
                filePairAction.actionType = ActionType.Delete;
                filePairAction.actionDirection = Direction.SourceToDestination;
                res = true;
            }
            else if (sourceFile != null && destFile == null)
            {
                filePairAction.actionType = ActionType.Delete;
                filePairAction.actionDirection = Direction.DestinationToSource;
                res = true;
            }
            return res;
        }

        private Dictionary<string, string> GetOldAndNewFile(string sourceFile, string destFile,
            Direction actionDirection)
        {
            var filesDict = new Dictionary<string, string>();

            string file1, file2;
            switch (actionDirection)
            {
                case Direction.SourceToDestination:
                    file1 = sourceFile;
                    file2 = destFile;
                    break;
                case Direction.DestinationToSource:
                    file1 = destFile;
                    file2 = sourceFile;
                    break;
                default:
                    throw new Exception("Invalid action direction!");
            }

            filesDict.Add("file1", file1);
            filesDict.Add("file2", file2);

            return filesDict;
        }
    }
}