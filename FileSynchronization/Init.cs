using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace FileSynchronization
{
    internal static class Init
    {
        internal static void InitializeFiles(SyncConfig confInstance)
        {
            var folderMappings = confInstance.FolderMappings;


            foreach (var pair in folderMappings)
            {
                PopulateSourceFiles(confInstance, pair.Key);
                PopulateDestinationFiles(confInstance, pair.Value);
            }

            PopulateFileMapping(confInstance);
        }

        private static void PopulateDestinationFiles(SyncConfig confInstance, string destinationFolder)
        {
            PopulateFileLists(destinationFolder, confInstance.DestinationFiles,FileType.Destination);
        }

        private static void PopulateSourceFiles(SyncConfig confInstance, string sourceFolder)
        {
            PopulateFileLists(sourceFolder, confInstance.SourceFiles,FileType.Source);
        }

        private static void PopulateFileLists(string path, List<FileExtended> fileInfos, FileType fileType)
        {
            string basePath = String.Copy(path);
            if (File.Exists(path))
            {
                // This path is a file
                ProcessFile(path, fileInfos, basePath, fileType);
            }
            else if (Directory.Exists(path))
            {
                // This path is a directory
                ProcessDirectory(path, fileInfos, basePath, fileType);
            }
        }

        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public static void ProcessDirectory(string targetDirectory, List<FileExtended> fileInfos, string basePath, FileType fileType)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName, fileInfos, basePath, fileType);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory, fileInfos, basePath, fileType);
        }


        public static void ProcessFile(string path, List<FileExtended> fileInfos, string basePath, FileType fileType)
        {
            var filePath = String.Copy(path);
            var fileExtended = new FileExtended();

            fileExtended.FileType = fileType;
            fileExtended.FileInfo = new FileInfo(filePath);
            fileExtended.FileID = Kernel32.GetCustomFileId(filePath);
            fileExtended.BasePath = basePath;
            
            fileInfos.Add(fileExtended);
            
        }


        internal static SyncConfig InitializeFolderMappings()
        {
            SyncConfig confInstance = new SyncConfig();
            try
            {
                var configReader = new AppSettingsReader();
                string configLocation = (string)configReader.GetValue("ConfigFile", typeof(string));


                XElement root = XElement.Load(configLocation);


                IEnumerable<XElement> mappingCollection = from m in root.Element("mappings").Elements("mapping")
                                                          select m;

                foreach (XElement el in mappingCollection)
                {
                    string sourceFolder = el.Element("SourceFolder").Value;
                    string destFolder = el.Element("DestinationFolder").Value;
                    confInstance.FolderMappings.Add(sourceFolder, destFolder);
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return confInstance;

        }

        private static void PopulateFileMappingFromCsv(SyncConfig confInstance)
        {
            // the assumption is that CSV file has the following structure:
            // <SourceFileType>,<SourceBasePath>,<SourceFullPath>,<DestFileType>,<DestBasePath>,<DestFullPath>

            var fileMapping = confInstance.FileMapping;
            var configReader = new AppSettingsReader();
            string fileMappingCsvLocation = (string)configReader.GetValue("FileID_mappings", typeof(string));

            if (File.Exists(fileMappingCsvLocation))
            {
                // Read data from CSV file
                using (CsvFileReader reader = new CsvFileReader(fileMappingCsvLocation))
                {
                    CsvRow row = new CsvRow();
                    while (reader.ReadRow(row))
                    {
                        var firstFileType = row[0];
                        var firstBasePath = row[1];
                        var firstFilePath = row[2];

                        var firstFileExtended = new FileExtended();
                        firstFileExtended.FileType = (FileType) Enum.Parse(typeof(FileType), firstFileType);
                        firstFileExtended.FileInfo = new FileInfo(firstFilePath);
                        firstFileExtended.BasePath = firstBasePath;
                        firstFileExtended.FileID = Kernel32.GetCustomFileId(firstFilePath);

                        var secondFileType = row[3];
                        var secondBasePath = row[4];
                        var secondFilePath = row[5];

                        var secondFileExtended = new FileExtended();
                        secondFileExtended.FileType = (FileType)Enum.Parse(typeof(FileType), secondFileType);
                        secondFileExtended.FileInfo = new FileInfo(secondFilePath);
                        secondFileExtended.BasePath = secondBasePath;
                        secondFileExtended.FileID = Kernel32.GetCustomFileId(secondFilePath);
                    }
                }
            }
        }

        private static void PopulateFileMappingFromPaths(SyncConfig confInstance)
        {
            if (confInstance.SourceFiles.Count > 0)
            {
                var sourceFiles = confInstance.SourceFiles;
                var destFiles = confInstance.DestinationFiles;
                var fileMapping = confInstance.FileMapping;

                foreach (var sourceFileExtended in sourceFiles)
                {
                    var sourceFilePath = sourceFileExtended.FileInfo.FullName;
                    string sourceRelativePath = sourceFileExtended.FileInfo.FullName.Replace(sourceFileExtended.BasePath,"");
                    var destFileExtended = destFiles.FirstOrDefault(x =>
                    {
                        string destRelativePath = x.FileInfo.FullName.Replace(x.BasePath, "");
                        return sourceRelativePath == destRelativePath;
                    });

                    fileMapping.Add(sourceFileExtended, destFileExtended);
                    
                }

                
                foreach (var destFileExtended in destFiles)
                {
                    var destFilePath = destFileExtended.FileInfo.FullName;
                    string destRelativePath = destFileExtended.FileInfo.FullName.Replace(destFileExtended.BasePath, "");
                    var sourceFileExtended = sourceFiles.FirstOrDefault(x =>
                    {
                        string sourceRelativePath = x.FileInfo.FullName.Replace(x.BasePath, "");
                        return sourceRelativePath == destRelativePath;
                    });

                    if (sourceFileExtended == null)
                    {
                        fileMapping.Add(destFileExtended, sourceFileExtended);
                    }
                }
                
            }
            else
            {
                throw new Exception("Source files not loaded yet");
            }
        }

        private static void PopulateFileMapping(SyncConfig confInstance)
        {
            PopulateFileMappingFromCsv(confInstance);
            if (confInstance.FileMapping.Count == 0)
            {
                PopulateFileMappingFromPaths(confInstance);
            }
        }
    }
}