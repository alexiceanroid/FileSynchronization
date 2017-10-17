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
            GoodSync execInstance = new GoodSync();
            InitializeFolderMappings(execInstance);
            
            
            
        }

        private static void InitializeFolderMappings(GoodSync execInstance)
        {
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
                execInstance.ResultStatus = "error";
                execInstance.ResultInfo = e.Message;
            }
        }
    }
   
}