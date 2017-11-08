using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace FileSynchronization
{


    public class SyncConfig
    {
        public Dictionary<string, string> FolderMappings;

        public SyncConfig()
        {
            FolderMappings = new Dictionary<string, string>();

            AppSettingsReader configReader = new AppSettingsReader();
            AppConfigLocation = (string)configReader.GetValue("ConfigFile_" + Environment.MachineName, typeof(string));

            LogFolder = GetConfigValueByName("LogFolder");
            MappingCsvFileName = GetConfigValueByName("FileMappingFile");

            var dateSuffix = DateTime.Now.Year + "-" + DateTime.Now.Month
                             + "-" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + ".log";
            ErrorLogFile = String.Copy(LogFolder) + @"\error_" + dateSuffix;
            ActionsPreviewLogFile = String.Copy(LogFolder) + @"\actions_preview_" + dateSuffix;
        }

        public string AppConfigLocation { get; }
        public string MappingCsvFileName { get; }
        public string LogFolder { get; }
        public string ErrorLogFile { get; }
        public string ActionsPreviewLogFile { get; set; }

        public void InitializeFolderMappings()
        {
            XElement root = XElement.Load(AppConfigLocation);
            var mappingCollection = root.Element("mappings").Elements("mapping");


            foreach (XElement el in mappingCollection)
            {
                string sourceFolder = el.Element("SourceFolder").Value;
                string sourceFolderResolved = DriveHelper.ResolvePath(this,sourceFolder);
                string destFolder = el.Element("DestinationFolder").Value;
                string destFolderResolved = DriveHelper.ResolvePath(this,destFolder);
                FolderMappings.Add(sourceFolderResolved, destFolderResolved);
            }
        }



        public string GetConfigValueByName(string name)
        {
            XElement root = XElement.Load(AppConfigLocation);
            var el = root.Element(name);


            if(el.HasElements)
               throw new XmlException("The node with name " + name 
                   + " contains other elements instead of string value");

            return el.Value;
        }
    }
 
}