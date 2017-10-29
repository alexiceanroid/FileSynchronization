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
        public Dictionary<FileExtended, FileExtended> FileMappingFromCsv { get; set; }
        public Dictionary<FileExtended, FileExtended> FileMappingFromPaths { get; set; }

        public Dictionary<string, string> FolderMappings;

        public SyncConfig()
        {
            FolderMappings = new Dictionary<string, string>();
            SourceFiles = new List<FileExtended>();
            DestinationFiles = new List<FileExtended>();
            FileMappingFromCsv = new Dictionary<FileExtended, FileExtended>();
            FileMappingFromPaths = new Dictionary<FileExtended, FileExtended>();
        }

        public Dictionary<FileExtended, FileExtended> FileMapping =>
            //(Dictionary<FileExtended, FileExtended>) 
            FileMappingFromCsv.Union(FileMappingFromPaths).ToDictionary(s => s.Key, s => s.Value);

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

        public FileExtended GetFileCounterpart(FileExtended f)
        {
            FileExtended resultingFile = null;
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
                    return file;
                }
            }

            return null;
        }

        public FileExtended GetFileByFullPath(string fullPath)
        {
            foreach (var file in SourceFiles)
            {
                if (file.fullPath == fullPath)
                {
                    return file;
                }
            }

            foreach (var file in DestinationFiles)
            {
                if (file.fullPath == fullPath)
                {
                    return file;
                }
            }

            return null;
        }

        public bool FileMappingMissingFiles()
        {
            return FilesMissingInMapping.Count > 0;
        }

        public List<FileExtended> FilesMissingInMapping
        {

            get
            {
                List<FileExtended> filesFromLists = SourceFiles.Union(DestinationFiles).ToList();
                List<FileExtended> filesFromMapping = FileMapping.Keys.Union(FileMapping.Values).ToList();

                return filesFromLists.Except(filesFromMapping).ToList();
            }
        }

        public FileExtended GetFileByIdOrPath(FileType fileType, string fileId, string fullPath)
        {
            FileExtended resultingFile;
            var fileFromId = GetFileById(fileType, fileId);
            //resultingFile = fileFromId ?? GetFileByFullPath(fullPath);
            if (fileFromId == null)
            {
                resultingFile = GetFileByFullPath(fullPath);
                resultingFile.fileID = Kernel32.GetCustomFileId(fullPath);
            }
            else
            {
                resultingFile = fileFromId;
            }

            return resultingFile;
        }

        
    }

    

    
}