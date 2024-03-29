﻿using System;
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
        public Dictionary<string, Tuple<string, string>> FolderMappings;
        public Dictionary<string, string> Parameters;
        public string AppConfigLocation { get; }
        public string ErrorLogFile { get; }
        public string ActionsPreviewLogFile { get; }
        public string SyncLog { get; }


        public SyncConfig()
        {
            FolderMappings = new Dictionary<string,Tuple<string, string>>();
            Parameters = new Dictionary<string, string>();
            

            AppSettingsReader configReader = new AppSettingsReader();
            AppConfigLocation = (string) configReader.GetValue("ConfigFile_" + Environment.MachineName,
                typeof(string));

            InitializeParameters();
            InitializeFolderMappings();
            
            var LogFolder = Parameters["LogFolder"];
            //MappingCsvFileName = GetConfigValueByName("FileMappingFile");

            var dateSuffix = DateTime.Now.Year + "-" + DateTime.Now.Month
                             + "-" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + ".log";
            ErrorLogFile = String.Copy(LogFolder) + @"\error_" + dateSuffix;
            ActionsPreviewLogFile = String.Copy(LogFolder) + @"\actions_preview_" + dateSuffix;
            SyncLog = String.Copy(LogFolder) + @"\sync_" + dateSuffix;
        }

        private void InitializeParameters()
        {
            XElement root = XElement.Load(AppConfigLocation);
            var parXml = root.Element("parameters").Elements("parameter");

            foreach (XElement par in parXml)
            {
                string pName = par.FirstAttribute.Value;
                string pVal = par.Value;

                Parameters.Add(pName, pVal);
            }
        }


        //public string MappingCsvFileName => Parameters["FileMappingFile"];
        //public string LogFolder => Parameters["LogFolder"];
        //public string ErrorLogFile 
        //{
        //    get
        //    {
        //        var dateSuffix = DateTime.Now.Year + "-" + DateTime.Now.Month
        //                         + "-" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute +
        //                         ".log";
        //        return String.Copy(LogFolder) + @"\error_" + dateSuffix;
        //    }
        //}

        

        public void InitializeFolderMappings()
        {
            XElement root = XElement.Load(AppConfigLocation);
            var mappingCollection = root.Element("mappings").Elements("mapping");


            foreach (XElement el in mappingCollection)
            {
                string mappingName = el.FirstAttribute.Value;
                string sourceFolder = el.Element("SourceFolder").Value;
                string destFolder = el.Element("DestinationFolder").Value;

                string sourceFolderResolved = DriveHelper.ResolvePath(this,sourceFolder);
                string destFolderResolved = DriveHelper.ResolvePath(this,destFolder);

                if (!Directory.Exists(sourceFolderResolved) || !Directory.Exists(destFolderResolved))
                    throw new DirectoryNotFoundException("Check source and destination folders: \n"
                                                         + sourceFolder + "\n"
                                                         + destFolder);

                var folderPair = new Tuple<string, string>(sourceFolderResolved,destFolderResolved);
                FolderMappings.Add(mappingName, folderPair);
            }

            if (FolderMappings.Count > 1)
                throw new ApplicationException("The app does not yet support more than one folder mapping");
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


        public bool AreBasePathsIncluded(string firstBasePath, string secondBasePath)
        {
            bool res = false;

            foreach (var folderPair in FolderMappings)
            {
                if (
                    (firstBasePath == folderPair.Value.Item1 && secondBasePath == folderPair.Value.Item2)
                    ||
                    (firstBasePath == folderPair.Value.Item2 && secondBasePath == folderPair.Value.Item1)
                )
                {
                    res = true;
                    break;
                }

            }


            return res;
        }
    }
 
}