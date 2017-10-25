using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.Runtime.InteropServices;

namespace FileSynchronization
{
    class Program
    {
        
        static void Main(string[] args)
        {

            var confInstance = PrepareSyncConfig();
            
            var syncExec = new SyncExecution(confInstance);

            syncExec.PopulateActionList();



        }

        static SyncConfig PrepareSyncConfig()
        {
            // read and initialize source and destination folder mappings:
            SyncConfig confInstance = Init.InitializeFolderMappings();

            // initialize source and destination files:
            Init.InitializeFiles(confInstance);
            Init.PopulateFileMapping(confInstance);
            return confInstance;
        }
    }

        
}
