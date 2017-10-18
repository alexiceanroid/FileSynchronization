using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using System.Xml;
using System.Xml.Linq;

namespace FileSynchronization
{
    class Program
    {
        static void Main(string[] args)
        {
            
            GoodSync execInstance = InitializeFolderMappings();
            
            // initialize source and destination files:
            InitializeFiles(execInstance);

        }

        private static void InitializeFiles(GoodSync execInstance)
        {
            var folderMappings = execInstance.FolderMappings;
            

            foreach (var pair in folderMappings)
            {
                PopulateSourceFiles(execInstance, pair.Key);
                PopulateDestinationFiles(execInstance, pair.Value);
            }
        }

        private static void PopulateDestinationFiles(GoodSync execInstance, string destinationFolder)
        {
            GetFileInfos(destinationFolder, execInstance.DestinationFiles);
        }

        private static void PopulateSourceFiles(GoodSync execInstance, string sourceFolder)
        {
            GetFileInfos(sourceFolder, execInstance.SourceFiles);
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


        private static GoodSync InitializeFolderMappings()
        {
            GoodSync execInstance = new GoodSync();
            try
            {
                var configReader = new AppSettingsReader();
                string configLocation = (string) configReader.GetValue("ConfigFile", typeof(string));


                XElement root = XElement.Load(configLocation);


                IEnumerable<XElement> mappingCollection = from m in root.Element("mappings").Elements("mapping")
                    select m;

                foreach (XElement el in mappingCollection)
                {
                    string sourceFolder = el.Element("SourceFolder").Value;
                    string destFolder = el.Element("DestinationFolder").Value;
                    execInstance.folderMappings.Add(sourceFolder, destFolder);
                }
            }
            catch (Exception e)
            {
                execInstance.ResultStatus = ResultStatusEnum.Error;
                execInstance.ResultInfo = e.Message;
            }
            
            return execInstance;
            
        }
    }
   
}