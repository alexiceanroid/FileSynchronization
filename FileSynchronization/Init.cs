using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace FileSynchronization
{

    internal static class Init
    {
        private static int _sourceFilesProcessed = 0;
        private static int _totalSourceFilesCount = 0;
        private static int _destFilesProcessed = 0;
        private static int _totalDestFilesCount = 0;
        private static string _additonalMappingFromPaths = "No";
        
        private static int _fileMappingCountPaths = 0;

        internal static void InitializeFiles(SyncConfig confInstance)
        {
            var folderMappings = confInstance.FolderMappings;
            var watchInitFiles = new Stopwatch();
            var watchSourceFiles = new Stopwatch();
            var watchDestFiles = new Stopwatch();

            watchInitFiles.Start();
            // count all files from source and destination:
            foreach (var pair in folderMappings)
            {
                _totalSourceFilesCount += Directory.GetFiles(pair.Key, "*.*", SearchOption.AllDirectories).Length;
                _totalDestFilesCount += Directory.GetFiles(pair.Value, "*.*", SearchOption.AllDirectories).Length;
            }

            Console.WriteLine("Starting populating source and destination files lists...");

            Task[] populatingFilesTask = new Task[2];
            populatingFilesTask[0] = Task.Factory.StartNew(() =>
            {
                watchSourceFiles.Start();
                confInstance.SourceFiles.Capacity = _totalSourceFilesCount;
                foreach (var pair in folderMappings)
                {
                    PopulateSourceFiles(confInstance, pair.Key);
                }
                watchSourceFiles.Stop();
            }
            );

            populatingFilesTask[1] = Task.Factory.StartNew(() =>
                {
                    watchDestFiles.Start();
                    confInstance.DestinationFiles.Capacity = _totalDestFilesCount;
                    foreach (var pair in folderMappings)
                    {
                        PopulateDestinationFiles(confInstance, pair.Value);
                    }
                    watchDestFiles.Stop();
                }
            );
            Task.WaitAll(populatingFilesTask);
            watchInitFiles.Stop();

            
            Console.WriteLine("Done:");
            Console.WriteLine($"Elapsed time: {FormatTime(watchInitFiles.ElapsedMilliseconds)}");
            Console.WriteLine($"\tprocessed {_sourceFilesProcessed} of {_totalSourceFilesCount} source files");
            Console.WriteLine($"\telapsed time: {FormatTime(watchSourceFiles.ElapsedMilliseconds)}");
            Console.WriteLine($"\tprocessed {_destFilesProcessed} of {_totalDestFilesCount} destination files");
            Console.WriteLine($"\telapsed time: {FormatTime(watchDestFiles.ElapsedMilliseconds)}");

        }

        public static string FormatTime(long milliseconds)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);
            string timeString = $"{timeSpan.Hours} h {timeSpan.Minutes} min {timeSpan.Seconds} sec";
            return timeString;
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
            var fileList = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);

            foreach (var file in fileList)
            {
                var filePath = String.Copy(file);
                var fileExtended = new FileExtended();

                fileExtended.FileType = fileType;
                fileExtended.FileInfo = new FileInfo(filePath);
                fileExtended.FileID = Kernel32.GetCustomFileId(filePath);
                fileExtended.BasePath = basePath;

                fileInfos.Add(fileExtended);
                if (fileType == FileType.Source)
                {
                    _sourceFilesProcessed++;
                }
                else
                {
                    _destFilesProcessed++;
                }
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
                var watchAddMissingFilesToMapping = new Stopwatch();
                watchAddMissingFilesToMapping.Start();
                var sourceFiles = confInstance.SourceFiles;
                var destFiles = confInstance.DestinationFiles;
                var fileMapping = confInstance.FileMapping;

                Console.WriteLine("\tStarting populating missing FileMapping from paths:");
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
                        _fileMappingCountPaths++;
                        Console.Write($"\r\tadded {_fileMappingCountPaths} file mappings from paths");
                    }
                    
                }
                
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
                            _fileMappingCountPaths++;
                            Console.Write($"\r\tadded {_fileMappingCountPaths} file mappings from paths");
                        }
                    }
                }
                Console.WriteLine("\tfinished populating missing FileMapping from paths");
                Console.WriteLine("\telapsed time: "+FormatTime(watchAddMissingFilesToMapping.ElapsedMilliseconds));

            }
            else
            {
                throw new Exception("Source files not loaded yet");
            }
        }

        internal static void PopulateFileMapping(SyncConfig confInstance)
        {
            var watchFileMapping = new Stopwatch();
            Console.WriteLine("\nStarting preparing file mapping...");
            watchFileMapping.Start();
            //confInstance.FileMapping
            CSVHelper.PopulateFileMappingFromCsv(confInstance);
            
            
            AddMissingFileMappingFromPaths(confInstance);
            if (_additonalMappingFromPaths == "Yes")
            {
                CSVHelper.SaveFileMappingToCsv(confInstance);
            }
            watchFileMapping.Stop();
            Console.WriteLine("File mapping complete!");
            Console.WriteLine($"elapsed time: {FormatTime(watchFileMapping.ElapsedMilliseconds)}");
        }
    }
}