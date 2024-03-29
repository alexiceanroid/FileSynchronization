﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FileSynchronization
{
    public partial class SyncExecution
    {
        private void ActionCreate(FileExtended sourceFileExtended, FileExtended destFileExtended, Direction actionDirection)
        {
            // declare fields for a file to be created
            FileType newFileType;
            string newFileBasePath;
            string newFileFullPath;
            string newFileLastWriteDate;
            string newFileId;
            int availableSpaceForFile;

            string sourceFile = sourceFileExtended != null ?
                sourceFileExtended.fullPath : "";
            string destFile = destFileExtended != null ?
                destFileExtended.fullPath : "";

            // determine which file needs copying: source or destination
            string fileToCopy;
            
            FileType fileType;
            string basePath;
            switch (actionDirection)
            {
                case Direction.SourceToDestination:
                    fileToCopy = sourceFile;
                    fileType = FileType.Source;
                    newFileType = FileType.Destination;
                    // get base path for this file:
                    basePath = sourceFileExtended.basePath;
                    break;
                case Direction.DestinationToSource:
                    fileToCopy = destFile;
                    fileType = FileType.Destination;
                    newFileType = FileType.Source;
                    // get base path for this file:
                    basePath = destFileExtended.basePath;
                    break;
                default:
                    throw new Exception("Invalid action direction for Create operation!");
            }

            

            switch (fileType)
            {
                case FileType.Source:
                    newFileBasePath = SyncConfig.FolderMappings.FirstOrDefault(x => x.Value.Item1 == basePath).Value.Item2;
                    break;
                case FileType.Destination:
                    newFileBasePath = SyncConfig.FolderMappings.FirstOrDefault(x => x.Value.Item2 == basePath).Value.Item1;
                    break;
                default:
                    throw new Exception("Invalid file type!");
            }

 
            // calculate new directory name where to copy the file to:
            newFileFullPath = fileToCopy.Replace(basePath, newFileBasePath);

            string targetPath = Path.GetDirectoryName(newFileFullPath);

            // Create a new target folder, if necessary.
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // Copy a file to another location and 
            // not overwrite the destination file if it already exists.
            
            File.Copy(fileToCopy, newFileFullPath, false);


            // assuming the file has been successfully copied, compute its ID
            // then update the corresponding entry in file mapping
            newFileId = Kernel32.GetCustomFileId(newFileFullPath);
            newFileLastWriteDate = (new FileInfo(newFileFullPath)).LastWriteTime.ToString(CultureInfo.InvariantCulture);

            // construct FileExtended instance for newly created file:
            var newFileExtended = new FileExtended(newFileType,newFileBasePath,newFileFullPath,
                newFileLastWriteDate,newFileId);

            // append source files or destination files with this new file:
            Dictionary<string,FileExtended> listToUpdate;
            switch (newFileType)
            {
                case FileType.Source:
                    listToUpdate = SourceFiles;
                    break;
                case FileType.Destination:
                    listToUpdate = DestFiles;
                    break;
                default:
                    throw new Exception("Invalid file type");
            }
            listToUpdate.Add(newFileExtended.fileID, newFileExtended);


            var fileToCopyInstance = GetFileByFullPath(fileToCopy);
            // update file mapping from paths
            FileMappingFromPaths[fileToCopyInstance] = newFileExtended;
        }

        private void ActionUpdate(FileExtended sourceFileExtended, FileExtended destFileExtended, Direction actionDirection)
        {
            string sourceFile = sourceFileExtended.fullPath;
            string destFile = destFileExtended.fullPath;
            
            FileExtended newFile = GetOldAndNewFile(sourceFile, destFile, actionDirection)["new"];
            FileExtended oldFile = GetOldAndNewFile(sourceFile, destFile, actionDirection)["old"];

            string newFileFullPath = newFile.fullPath;
            string oldFileFullPath = oldFile.fullPath;

            // update file2 with file1
            // update direction: newFileFullPath => oldFileFullPath
            File.Copy(newFileFullPath, oldFileFullPath, true);
            
        }

        

        private void ActionRenameMove(FileExtended sourceFileExtended, FileExtended destFileExtended, Direction actionDirection)
        {
            string sourceFile = sourceFileExtended.fullPath;
            string destFile = destFileExtended.fullPath;
            try
            {
                var newFile = GetOldAndNewFile(sourceFile, destFile, actionDirection)["new"];
                var oldFile = GetOldAndNewFile(sourceFile, destFile, actionDirection)["old"];

                string oldFileExpectedFullPath = oldFile.basePath + newFile.RelativePath;
                string oldFileExpectedDirectory = Path.GetDirectoryName(oldFileExpectedFullPath);

                if (!Directory.Exists(oldFileExpectedDirectory))
                    Directory.CreateDirectory(oldFileExpectedDirectory);

                File.Move(oldFile.fullPath, oldFileExpectedFullPath);

                var oldFileExpected = new FileExtended(oldFile.fileType,oldFile.basePath,
                    oldFileExpectedFullPath, oldFile.fileID);

                UpdateFileInMapping(oldFile, oldFileExpected);

            }
            catch (Exception ex)
            {
                throw new Exception("Error occured during move operation: \n" + ex.Message);
            }
        }

        private void ActionDelete(FileExtended sourceFileExtended, FileExtended destFileExtended,
            Direction actionDirection)
        {
            FileExtended fileToDelete = null;
            switch (actionDirection)
            {
                case Direction.SourceToDestination:
                    fileToDelete = destFileExtended;
                    FileMappingFromCsv.Remove(destFileExtended);
                    break;
                case Direction.DestinationToSource:
                    fileToDelete = sourceFileExtended;
                    FileMappingFromCsv.Remove(sourceFileExtended);
                    break;
            }

            if (fileToDelete != null)
            {
                
                string pathForArchival = Path.Combine(SyncConfig.Parameters["ArchiveFolder"], DateTime.Now.ToString("yyyy-MM-dd"));
                string logFile = SyncConfig.SyncLog;
                WorkingWithFiles.ArchiveFile(fileToDelete, logFile, pathForArchival,"deletion");
            }
        }
    }
}