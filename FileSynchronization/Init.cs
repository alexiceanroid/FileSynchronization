using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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
        }

        private static void PopulateDestinationFiles(SyncConfig confInstance, string destinationFolder)
        {
            GetFileInfos(destinationFolder, confInstance.DestinationFiles);
        }

        private static void PopulateSourceFiles(SyncConfig confInstance, string sourceFolder)
        {
            GetFileInfos(sourceFolder, confInstance.SourceFiles);
        }

        private static void GetFileInfos(string path, HashSet<FileInfo> fileInfos)
        {

            if (File.Exists(path))
            {
                // This path is a file
                ProcessFile(path, fileInfos);
            }
            else if (Directory.Exists(path))
            {
                // This path is a directory
                ProcessDirectory(path, fileInfos);
            }
        }

        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public static void ProcessDirectory(string targetDirectory, HashSet<FileInfo> fileInfos)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName, fileInfos);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory, fileInfos);
        }


        public static void ProcessFile(string path, HashSet<FileInfo> fileInfos)
        {
            FileInfo file = new FileInfo(path);
            fileInfos.Add(file);
            
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
    }
}