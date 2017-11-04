using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FileSynchronization
{


    public class SyncConfig
    {
        private string _appConfigLocation;
        private string _mappingCsvFileName;
        private string _logFolder;
        private string _errorLogFile;

        public Dictionary<string, string> FolderMappings;

        public SyncConfig()
        {
            FolderMappings = new Dictionary<string, string>();

            AppSettingsReader configReader = new AppSettingsReader();
            _mappingCsvFileName = (string)configReader.GetValue("FileID_mappings_" + Environment.MachineName, typeof(string));
            _appConfigLocation = (string)configReader.GetValue("ConfigFile_" + Environment.MachineName, typeof(string));
            _logFolder = (string)configReader.GetValue("LogFolder_" + Environment.MachineName, typeof(string));

            _errorLogFile = String.Copy(_logFolder) + @"\" + DateTime.Now.Year + "-" + DateTime.Now.Month 
                + "-" + DateTime.Now.Day + "_" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ".log";
        }

        public string AppConfigLocation => _appConfigLocation;
        public string MappingCsvFileName => _mappingCsvFileName;
        public string LogFolder => _logFolder;
        public string ErrorLogFile => _errorLogFile;

        public void InitializeFolderMappings()
        {
            XElement root = XElement.Load(_appConfigLocation);


            IEnumerable<XElement> mappingCollection = from m in root.Element("mappings").Elements("mapping")
                select m;

            foreach (XElement el in mappingCollection)
            {
                string sourceFolder = el.Element("SourceFolder").Value;
                string sourceFolderResolved = DriveHelper.ResolvePath(this,sourceFolder);
                string destFolder = el.Element("DestinationFolder").Value;
                string destFolderResolved = DriveHelper.ResolvePath(this,destFolder);
                FolderMappings.Add(sourceFolderResolved, destFolderResolved);
            }
        }
    }
 
}