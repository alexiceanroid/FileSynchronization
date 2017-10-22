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
        private static int _filesProcessed = 0;
        private static int _totalFilesCount = 0;
        private static string _additonalMappingFromPaths = "No";

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
            _filesProcessed = 0;
            Console.WriteLine("Starting populating destination files list...");
            PopulateFileLists(destinationFolder, confInstance.DestinationFiles,FileType.Destination);
            Console.WriteLine("\nFinished populating destination files");
        }

        private static void PopulateSourceFiles(SyncConfig confInstance, string sourceFolder)
        {
            Console.WriteLine("Starting populating source files list...");
            PopulateFileLists(sourceFolder, confInstance.SourceFiles,FileType.Source);
            Console.WriteLine("\nFinished populating source files");
        }

        private static void PopulateFileLists(string path, List<FileExtended> fileInfos, FileType fileType)
        {
            string basePath = String.Copy(path);
            var fileList = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);
            _totalFilesCount = fileList.Length;


            foreach (var file in fileList)
            {
                var filePath = String.Copy(file);
                var fileExtended = new FileExtended();

                fileExtended.FileType = fileType;
                fileExtended.FileInfo = new FileInfo(filePath);
                fileExtended.FileID = Kernel32.GetCustomFileId(filePath);
                fileExtended.BasePath = basePath;

                fileInfos.Add(fileExtended);
                _filesProcessed++;
                Console.Write($"\r processed {_filesProcessed} of {_totalFilesCount} files    ");
            }
            
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
                    string sourceFolderResolved = DriveHelper.ResolvePath(sourceFolder);
                    string destFolder = el.Element("DestinationFolder").Value;
                    string destFolderResolved = DriveHelper.ResolvePath(destFolder);
                    confInstance.FolderMappings.Add(sourceFolderResolved, destFolderResolved);
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return confInstance;

        }

        

        private static void AddMissingFileMappingFromPaths(SyncConfig confInstance)
        {
            if (confInstance.SourceFiles.Count > 0)
            {
                var sourceFiles = confInstance.SourceFiles;
                var destFiles = confInstance.DestinationFiles;
                var fileMapping = confInstance.FileMapping;

                Console.WriteLine("\nStarting populating FileMapping from paths:");
                foreach (var sourceFileExtended in sourceFiles)
                {
                    // check if FileID of current sourceFileExtended already exists in fileMapping:
                    var sourcePresentFileID = fileMapping.Keys.FirstOrDefault(x => x.FileID == sourceFileExtended.FileID);
                    if (sourcePresentFileID == null)
                    {
                        _additonalMappingFromPaths = "Yes";
                        var sourceFilePath = sourceFileExtended.FileInfo.FullName;
                        string sourceRelativePath =
                            sourceFileExtended.FileInfo.FullName.Replace(sourceFileExtended.BasePath, "");
                        var destFileExtended = destFiles.FirstOrDefault(x =>
                        {
                            string destRelativePath = x.FileInfo.FullName.Replace(x.BasePath, "");
                            return sourceRelativePath == destRelativePath;
                        });

                        fileMapping.Add(sourceFileExtended, destFileExtended);
                        _filesProcessed++;
                        Console.Write($"\r additional files processed: {_filesProcessed}");
                    }
                    
                }
                Console.WriteLine("");
                
                foreach (var destFileExtended in destFiles)
                {
                    // check if FileID of current destFileExtended already exists in fileMapping 
                    // (this time checking both keys and values)
                    var destPresentFileIdLeft = fileMapping.Keys.FirstOrDefault(x => x.FileID == destFileExtended.FileID);
                    var destPresentFileIdRight = fileMapping.Keys.FirstOrDefault(x => x.FileID == destFileExtended.FileID);
                    
                    // if it does not exist in keys and values then perform mapping based on paths:
                    if (destPresentFileIdLeft == null && destPresentFileIdRight == null)
                    {
                        _additonalMappingFromPaths = "Yes";
                        var destFilePath = destFileExtended.FileInfo.FullName;
                        string destRelativePath =
                            destFileExtended.FileInfo.FullName.Replace(destFileExtended.BasePath, "");
                        var sourceFileExtended = sourceFiles.FirstOrDefault(x =>
                        {
                            string sourceRelativePath = x.FileInfo.FullName.Replace(x.BasePath, "");
                            return sourceRelativePath == destRelativePath;
                        });

                        if (sourceFileExtended == null)
                        {
                            fileMapping.Add(destFileExtended, sourceFileExtended);
                            _filesProcessed++;
                            Console.Write($"\r additional files processed: {_filesProcessed}");
                        }
                    }
                }
                Console.WriteLine("\n\nFinished populating FileMapping\n");

            }
            else
            {
                throw new Exception("Source files not loaded yet");
            }
        }

        private static void PopulateFileMapping(SyncConfig confInstance)
        {
            CSVHelper.PopulateFileMappingFromCsv(confInstance);
            
            _filesProcessed = 0;
            AddMissingFileMappingFromPaths(confInstance);
            if (_additonalMappingFromPaths == "Yes")
            {
                CSVHelper.SaveFileMappingToCsv(confInstance);
            }
        }
    }
}