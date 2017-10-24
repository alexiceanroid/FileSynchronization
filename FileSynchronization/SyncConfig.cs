using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{


    public class SyncConfig
    {
        public string AppConfigLocation;
        public string FileMappingCsvLocation;
        public List<FileExtended> SourceFiles { get; set; }
        public List<FileExtended> DestinationFiles { get; set; }
        public Dictionary<FileExtended, FileExtended?> FileMapping { get; set; }
        
        public Dictionary<string, string> FolderMappings;

        public SyncConfig()
        {
            FolderMappings = new Dictionary<string, string>();
            SourceFiles = new List<FileExtended>();
            DestinationFiles = new List<FileExtended>();
            FileMapping = new Dictionary<FileExtended, FileExtended?>();
        }


        public bool FilesMatchBasedOnPaths(FileExtended f1, FileExtended f2)
        {
            var filesMatch = false;
            var basePathsCompatible = false;
            foreach (var folderMapping in FolderMappings)
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

        public FileExtended? GetFileMatch(FileExtended f)
        {
            FileExtended? resultingFile = null;
            List<FileExtended> filesListToSearch;
            filesListToSearch = f.fileType == FileType.Source ? DestinationFiles : SourceFiles;
            
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
            FileExtended resultingFile = new FileExtended();
            List<FileExtended> listToSearch;
            switch (fileType)
            {
                case FileType.Source:
                    listToSearch = SourceFiles;
                    break;
                case FileType.Destination:
                    listToSearch = DestinationFiles;
                    break;
                default:
                    throw new Exception("Unknown file type!");
            }

            foreach (var file in listToSearch)
            {
                if (file.fileID == id)
                {
                    resultingFile = file;
                }
            }

            return resultingFile;
        }
        
        
    }

    public struct FileExtended
    {
        public readonly FileType fileType;
        public readonly string basePath;
        public readonly string fullPath;
        public readonly string lastWriteDateTime;
        public readonly string fileID;

        public FileExtended(FileType _fileType, string _basePath, string _fullPath, string _lastWriteDate, string _fileId)
        {
            fileType = _fileType;
            basePath = _basePath;
            fullPath = _fullPath;
            lastWriteDateTime = _lastWriteDate;
            fileID = _fileId;
        }

        public string RelativePath
        {
            get
            {
                if (!String.IsNullOrEmpty(fileID))
                {
                    return fullPath.Replace(basePath, "");
                }
                else
                {
                    return null;
                }
            }
            
            
        }

        

        public override int GetHashCode()
        {
            return this.fileID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is FileExtended externalFile)
            {
                return this.GetHashCode() == externalFile.GetHashCode();
            }
            else
            {
                return false;
            }
        }
    }

    public enum FileType
    {
        Source,
        Destination
    }
}