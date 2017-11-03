using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace FileSynchronization
{

    public static class Init
    {
        private static int _totalSourceFilesCount = 0;
        private static int _totalDestFilesCount = 0;
        private static string _additonalMappingFromPaths = "No";
        
        private static int _fileMappingCountPaths = 0;

        public static void InitializeFiles(SyncExecution syncExec)
        {
            var folderMappings = syncExec.SyncConfig.FolderMappings;
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
                syncExec.SourceFiles.Capacity = _totalSourceFilesCount;
                foreach (var pair in folderMappings)
                {
                    PopulateSourceFiles(syncExec, pair.Key);
                }
                watchSourceFiles.Stop();
            }
            );

            populatingFilesTask[1] = Task.Factory.StartNew(() =>
                {
                    watchDestFiles.Start();
                    syncExec.DestFiles.Capacity = _totalDestFilesCount;
                    foreach (var pair in folderMappings)
                    {
                        PopulateDestFiles(syncExec, pair.Value);
                    }
                    watchDestFiles.Stop();
                }
            );
            Task.WaitAll(populatingFilesTask);
            watchInitFiles.Stop();

            
            Console.WriteLine("Done:");
            Console.WriteLine($"Elapsed time: {FormatTime(watchInitFiles.ElapsedMilliseconds)}");
            //Console.WriteLine($"\tprocessed {_sourceFilesProcessed} of {_totalSourceFilesCount} source files");
            //Console.WriteLine($"\telapsed time: {FormatTime(watchSourceFiles.ElapsedMilliseconds)}");
            //Console.WriteLine($"\tprocessed {_destFilesProcessed} of {_totalDestFilesCount} destination files");
            //Console.WriteLine($"\telapsed time: {FormatTime(watchDestFiles.ElapsedMilliseconds)}");

        }

        public static string FormatTime(long milliseconds)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);
            string timeString = $"{timeSpan.Hours} h {timeSpan.Minutes} min {timeSpan.Seconds} sec";
            return timeString;
        }

        private static void PopulateDestFiles(SyncExecution syncExec, string destinationFolder)
        {
            
            PopulateFileLists(destinationFolder, syncExec.DestFiles,FileType.Destination);
        }

        private static void PopulateSourceFiles(SyncExecution syncExec, string sourceFolder)
        {
            PopulateFileLists(sourceFolder, syncExec.SourceFiles,FileType.Source);
        }

        private static void PopulateFileLists(string path, List<FileExtended> fileInfos, FileType fileType)
        {
            string basePath = String.Copy(path);
            var fileList = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);

            foreach (var file in fileList)
            {
                var filePath = String.Copy(file);
                var fileInfo = new FileInfo(filePath);
                var fileExtended = new FileExtended
                (
                    fileType,
                    basePath,
                    fileInfo.FullName,
                    fileInfo.LastWriteTime.ToString(CultureInfo.InvariantCulture),
                    Kernel32.GetCustomFileId(filePath)
                );

                fileInfos.Add(fileExtended);
            }
            
        }

        

        public static SyncConfig InitializeFolderMappings()
        {
            SyncConfig confInstance = new SyncConfig();
            try
            {
                var configReader = new AppSettingsReader();
                string configLocation = (string)configReader.GetValue("ConfigFile_"+Environment.MachineName, typeof(string));
                confInstance.AppConfigLocation = configLocation;


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

        

        private static void AddMissingFileMappingFromPaths(SyncExecution syncExec)
        {
            
            
            if (syncExec.SourceFiles.Count > 0)
            {
                var watchAddMissingFilesToMapping = new Stopwatch();
                watchAddMissingFilesToMapping.Start();
                var sourceFiles = syncExec.SourceFiles;
                var destFiles = syncExec.DestFiles;
                var fileMappingFromPaths = syncExec.FileMappingFromPaths;
                var fileMapping = syncExec.FileMapping;

                Console.WriteLine();
                Console.WriteLine("\tStarting populating missing FileMapping from paths:");
                foreach (var missingFile in syncExec.FilesMissingInMapping)
                {
                    _additonalMappingFromPaths = "Yes";
                        
                    fileMappingFromPaths.Add(missingFile, syncExec.GetFileCounterpart(missingFile));
                    _fileMappingCountPaths++;
                    Console.Write($"\r\tadded {_fileMappingCountPaths} file mappings from paths");

                    if (syncExec.FilesMissingInMapping.Count == 0)
                        break;
                }
                
                
                Console.WriteLine("\n\tfinished populating missing FileMapping from paths. Added " + fileMappingFromPaths.Count + " entries.");
                Console.WriteLine("\telapsed time: "+FormatTime(watchAddMissingFilesToMapping.ElapsedMilliseconds));

            }
            else
            {
                throw new Exception("Source files not loaded yet");
            }
        }

        public static void InitFileMapping(SyncExecution syncExec)
        {
            var watchFileMapping = new Stopwatch();
            Console.WriteLine("\nStarting preparing file mapping...");
            watchFileMapping.Start();

            CSVHelper.InitFileMappingFromCsv(syncExec);

            bool csvExists = File.Exists(syncExec.FileMappingCsvLocation);
            DateTime csvLastWrite = DateTime.MinValue;
            DateTime appConfLastWrite = DateTime.MinValue;
            if (csvExists)
            {
                csvLastWrite = (new FileInfo(syncExec.FileMappingCsvLocation)).LastWriteTime;
                appConfLastWrite = (new FileInfo(syncExec.SyncConfig.AppConfigLocation)).LastWriteTime;
            }
            
            // append existing file mapping if app_config has been modified later than csv mapping file
            // or if csv file does not exist
            if(appConfLastWrite > csvLastWrite ||
                !csvExists
                ||
               syncExec.FileMappingMissingFiles()
              )
            { 
                AddMissingFileMappingFromPaths(syncExec);
            }
            watchFileMapping.Stop();
            Console.WriteLine("File mapping complete!");
            Console.WriteLine($"elapsed time: {FormatTime(watchFileMapping.ElapsedMilliseconds)}");
        }
    }
}