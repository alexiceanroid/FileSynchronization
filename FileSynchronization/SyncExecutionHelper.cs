using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public partial class SyncExecution
    {

        private bool AddCreateAction(KeyValuePair<FileExtended,FileExtended> filePair)
        {
            bool create = false;

            if (filePair.Value == null)
            {
                create = true;
                FilePairAction filePairAction = new FilePairAction(filePair.Key, filePair.Value);
                filePairAction.ActionType = ActionType.Create;
                filePairAction.ActionDirection = filePairAction.File1.fileType == FileType.Source ? 
                    Direction.SourceToDestination : Direction.DestinationToSource;

                AddFilePairWithCheck(filePairAction);
                
            }

            return create;
        }

        


        private void AddUpdateAction(KeyValuePair<FileExtended,FileExtended> filePair)
        {

            var file1 = filePair.Key;
            var file2 = filePair.Value;
            if (file1 != null && file2 != null)
            {
                // Action = Update
                DateTime file1LastUpdatedOn = DateTime.Parse(file1.lastWriteDateTime, CultureInfo.InvariantCulture);
                DateTime file2LastUpdatedOn = DateTime.Parse(file2.lastWriteDateTime, CultureInfo.InvariantCulture);
                if (file1LastUpdatedOn.Date != file2LastUpdatedOn.Date
                    || file1LastUpdatedOn.TimeOfDay != file2LastUpdatedOn.TimeOfDay)
                {
                    FilePairAction filePairAction = new FilePairAction(filePair.Key, filePair.Value)
                    {
                        ActionType = ActionType.Update
                    };
                    if (file1LastUpdatedOn > file2LastUpdatedOn)
                    {
                        filePairAction.ActionDirection = file1.fileType == FileType.Source
                            ? Direction.SourceToDestination
                            : Direction.DestinationToSource;
                    }
                    else
                    {
                        filePairAction.ActionDirection = file1.fileType == FileType.Source
                            ? Direction.DestinationToSource
                            : Direction.SourceToDestination;
                    }

                    AddFilePairWithCheck(filePairAction);
                }
            }

        }




        private void AddFilePairWithCheck(FilePairAction filePairAction)
        {
            //bool filePairAddedAlready = false;

            //foreach (var entry in actionList)
            //{
            //    if (filePairAction.File1 == entry.File1
            //        &&
            //        filePairAction.File2 == entry.File2)
            //    {
            //        filePairAddedAlready = true;
            //        break;
            //    }

            //}

            //if (filePairAddedAlready)
            //{
            //    string mes = "The actions list already contains this file combination:\n" +
            //                 "\t source file:      " + filePairAction.File1.fullPath + "\n" +
            //                 "\t destination file: " + filePairAction.File2.fullPath;

            //    throw new Exception(mes);
            //}
            _actionList.Add(filePairAction);
        }

        public void AppendActionListWithDeleteRenameMove(FileExtended firstFileExtended, FileExtended secondFileExtended, 
            CsvRow row)
        {
            
            var filePairAction = new FilePairAction(firstFileExtended, secondFileExtended);



            FileExtended sourceFile = WorkingWithFiles.GetSourceAndDestFile(firstFileExtended, secondFileExtended)[FileType.Source];
            FileExtended destFile   = WorkingWithFiles.GetSourceAndDestFile(firstFileExtended, secondFileExtended)[FileType.Destination];

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
            if(filePairAction.ActionType == ActionType.RenameMove
                ||
                filePairAction.ActionType == ActionType.Rename
                ||
               filePairAction.ActionType == ActionType.Move
                ||
               filePairAction.ActionType == ActionType.Delete)
            { AddFilePairWithCheck(filePairAction);}
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
                    &&
                    !(sourceName == destName && sourceFile.RelativePath == destFile.RelativePath)
                )

            {
                filePairAction.ActionType = ActionType.RenameMove;

                if (sourceFile.fullPath != oldSourceFile.fullPath
                    &&
                    destFile.fullPath == oldDestFile.fullPath)
                {
                    filePairAction.ActionDirection = Direction.SourceToDestination;
                }
                else if (sourceFile.fullPath == oldSourceFile.fullPath
                         &&
                         destFile.fullPath != oldDestFile.fullPath)
                {
                    filePairAction.ActionDirection = Direction.DestinationToSource;
                }
                else if (sourceFile.fullPath != oldSourceFile.fullPath
                         &&
                         destFile.fullPath != oldDestFile.fullPath)
                {
                    filePairAction.ActionDirection = Direction.Unknown;
                }

                return;
            }

            // identify Rename
            if (
                (sourceName != oldSourceName || destName != oldDestName)
                &&
                (sourceName != destName)
                )
            {
                filePairAction.ActionType = ActionType.Rename;

                if (sourceName != oldSourceName
                    && destName == oldDestName)
                {
                    filePairAction.ActionDirection = Direction.SourceToDestination;
                }
                else if (sourceName == oldSourceName
                         && destName != oldDestName)
                {
                    filePairAction.ActionDirection = Direction.DestinationToSource;
                }
                else if (sourceName != oldSourceName
                         && destName != oldDestName)
                {
                    filePairAction.ActionDirection = Direction.Unknown;
                }
                return;
            }

            // identify Move
            if (
                (sourceDirectory != oldSourceDirectory || destDirectory != oldDestDirectory)
                &&
                sourceFile.RelativePath != destFile.RelativePath
                )
            {
                filePairAction.ActionType = ActionType.Move;

                if (sourceDirectory != oldSourceDirectory
                    && destDirectory == oldDestDirectory)
                {
                    filePairAction.ActionDirection = Direction.SourceToDestination;
                }
                else if (sourceDirectory == oldSourceDirectory
                         && destDirectory != oldDestDirectory)
                {
                    filePairAction.ActionDirection = Direction.DestinationToSource;
                }
                else if (sourceDirectory != oldSourceDirectory
                         && destDirectory != oldDestDirectory)
                {
                    filePairAction.ActionDirection = Direction.Unknown;
                }
            }
        }

        private bool IdentifyDeletion(FileExtended sourceFile, FileExtended destFile, 
            FilePairAction filePairAction)
        {
            bool res = false;

            if (sourceFile == null && destFile != null)
            {
                filePairAction.ActionType = ActionType.Delete;
                filePairAction.ActionDirection = Direction.SourceToDestination;
                res = true;
            }
            else if (sourceFile != null && destFile == null)
            {
                filePairAction.ActionType = ActionType.Delete;
                filePairAction.ActionDirection = Direction.DestinationToSource;
                res = true;
            }
            return res;
        }

        private Dictionary<string, FileExtended> GetOldAndNewFile(string sourceFile, string destFile,
            Direction actionDirection)
        {
            var filesDict = new Dictionary<string, FileExtended>();

            var sourceFileId = Kernel32.GetCustomFileId(sourceFile);
            var destFileId = Kernel32.GetCustomFileId(destFile);

            var sourceFileExtended = SourceFiles[sourceFileId];
            var destFileExtended = DestFiles[destFileId];

            FileExtended file1, file2;
            switch (actionDirection)
            {
                case Direction.SourceToDestination:
                    file1 = sourceFileExtended;
                    file2 = destFileExtended;
                    break;
                case Direction.DestinationToSource:
                    file1 = destFileExtended;
                    file2 = sourceFileExtended;
                    break;
                default:
                    throw new Exception("Invalid action direction!");
            }

            filesDict.Add("new", file1);
            filesDict.Add("old", file2);

            return filesDict;
        }

        //internal bool ActionListContainsFilePairAction(KeyValuePair<FileExtended,FileExtended> filePair)
        //{
        //    bool res = false;
        //    foreach (var action in actionList)
        //    {
        //        if (
        //            action.ActionType == filePair.
        //                ((action.File1 == filePair.Key && action.File2 == filePair.Value)
        //                ||
        //                (action.File1 == filePair.Value && action.File2 == filePair.Key)
        //                )
        //            )
        //        {
        //            res = true;
        //            break;
        //        }
        //    }

        //    return res;
        //}

        internal void UpdateFileInMapping(FileExtended oldFile, FileExtended newFile)
        {
            FileExtended counterpartFile;
            if (FileMappingFromCsv.ContainsKey(oldFile))
            {
                counterpartFile = FileMappingFromCsv[oldFile];
                FileMappingFromCsv.Remove(oldFile);
                FileMappingFromCsv.Add(newFile,counterpartFile);
            }
            else if (FileMappingFromCsv.ContainsValue(oldFile))
            {
                counterpartFile = FileMappingFromCsv.FirstOrDefault(x => x.Value == oldFile).Key;
                FileMappingFromCsv[counterpartFile] = newFile;
            }
            else
            {
                throw new Exception("Could not find specified file in mapping: \n" + oldFile.fullPath);
            }
        }

        internal void DisplaySyncProcessStats(string currentFileOperation)
        {
            int filesProcessed = _filesCreated + _filesDeleted + _filesRenamedMoved 
                + _filesMoved + _filesRenamed + _filesUpdated;

            int w = Console.WindowWidth;
            int textLinesSpan = (int)Math.Ceiling((double)currentFileOperation.Length / w);
            int verticalOffset = 7 + textLinesSpan;
            if (filesProcessed > 0)
            {
                
                Console.SetCursorPosition(0, Console.CursorTop - verticalOffset);
            }
            Console.Write("\r" + currentFileOperation.PadRight(textLinesSpan * w-1) + "\n"
                          + ("files created:           " + _filesCreated).PadRight(w - 1) + "\n"
                          + ("files updated:           " + _filesUpdated).PadRight(w - 1) + "\n"
                          + ("files renamed and moved: " + _filesRenamedMoved).PadRight(w - 1) + "\n"
                          + ("files renamed:           " + _filesRenamed).PadRight(w - 1) + "\n"
                          + ("files moved:             " + _filesMoved).PadRight(w - 1) + "\n"
                          + ("files deleted:           " + _filesDeleted).PadRight(w - 1) + "\n\n"
                          + "completion percentage:   " + Math.Round(100*(double)filesProcessed/ActionsList.Count) + "%") ;
            
        }

        private void AddMoveAction(KeyValuePair<FileExtended,FileExtended> filePair)
        {
            var file1 = filePair.Key;
            var file2 = filePair.Value;

            if (file1 != null && file2 != null)
            {
                if (file1.RelativePath != file2.RelativePath)
                {
                    FilePairAction filePairAction = new FilePairAction(filePair.Key, filePair.Value);
                    filePairAction.ActionType = ActionType.Move;

                    DateTime lastWriteDate1 = DateTime.Parse(file1.lastWriteDateTime, CultureInfo.InvariantCulture);
                    DateTime lastWriteDate2 = DateTime.Parse(file2.lastWriteDateTime, CultureInfo.InvariantCulture);

                    if (lastWriteDate1.Date == lastWriteDate2.Date
                        &&
                        lastWriteDate1.TimeOfDay == lastWriteDate2.TimeOfDay)
                    {
                        filePairAction.ActionDirection = Direction.SourceToDestination;
                    }
                    else
                    {
                        filePairAction.ActionDirection = lastWriteDate1 < lastWriteDate2
                            ? Direction.DestinationToSource
                            : Direction.SourceToDestination;
                    }

                    AddFilePairWithCheck(filePairAction);
                }
            }
        }
    }
}