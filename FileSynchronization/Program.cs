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
            

            InitializeFiles(execInstance);

        }

        private static void InitializeFiles(GoodSync execInstance)
        {
            var folderMappings = execInstance.folderMappings;
            

            foreach (var pair in folderMappings)
            {
                PopulateSourceFiles(execInstance, pair.Key);
                PopulateDestinationFiles(execInstance, pair.Value);
                PopulateSourceFilesToProcess(execInstance);
            }
        }

        private static void PopulateSourceFiles(GoodSync execInstance, string sourceFolder)
        {
            if (File.Exists(sourceFolder))
            {
                // This path is a file
                ProcessFile(sourceFolder);
            }
            else if (Directory.Exists(sourceFolder))
            {
                // This path is a directory
                ProcessDirectory(sourceFolder);
            }
        }

        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public static void ProcessDirectory(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        // Insert logic for processing found files here.
        public static void ProcessFile(string path)
        {
            
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