using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FileSynchronization
{
    public partial class SyncExecution
    {
        private void ActionCreate(string sourceFile, string destFile, Direction actionDirection)
        {
            // declare fields for a file to be created
            FileType newFileType;
            string newFileBasePath;
            string newFileFullPath;
            string newFileLastWriteDate;
            string newFileId;

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
                    // get base path for this file by querying file mapping:
                    basePath = SourceFiles.FirstOrDefault(x => x.fullPath == fileToCopy).basePath;
                    break;
                case Direction.DestinationToSource:
                    fileToCopy = destFile;
                    fileType = FileType.Destination;
                    newFileType = FileType.Source;
                    // get base path for this file by querying file mapping:
                    basePath = DestFiles.FirstOrDefault(x => x.fullPath == fileToCopy).basePath;
                    break;
                default:
                    throw new Exception("Invalid action direction for Create operation!");
            }

            

            switch (fileType)
            {
                case FileType.Source:
                    newFileBasePath = _syncConfig.FolderMappings.FirstOrDefault(x => x.Key == basePath).Value;
                    break;
                case FileType.Destination:
                    newFileBasePath = _syncConfig.FolderMappings.FirstOrDefault(x => x.Value == basePath).Key;
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
            List<FileExtended> listToUpdate;
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
            listToUpdate.Add(newFileExtended);


            var fileToCopyInstance = _syncConfig.GetFileByFullPath(fileToCopy);
            // update file mapping from paths
            FileMappingFromPaths[fileToCopyInstance] = newFileExtended;
        }

        private void ActionUpdate(string sourceFile, string destFile, Direction actionDirection)
        {
            
        }

        private Dictionary<FileType,string> GetSourceAndDestFile(FilePairAction action)
        {
            var files = new Dictionary<FileType, string>();
            string sourceFile = "";
            string destFile = "";

            if (action._file1.fileType == FileType.Source)
            {
                sourceFile = action._file1 != null ? action._file1.fullPath : "";
                destFile = action._file2 != null? action._file2.fullPath : "";
            }
            else
            {
                destFile = action._file1 != null ? action._file1.fullPath : "";
                sourceFile = action._file2 != null ? action._file2.fullPath : "";
            }
            files.Add(FileType.Source, sourceFile);
            files.Add(FileType.Destination, destFile);

            return files;
        }
    }
}