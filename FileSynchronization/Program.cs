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

namespace FileSynchronization
{
    class Program
    {
        private static FileSystemWatcher watcher;
        static void Main(string[] args)
        {
            // read and initialize source and destination folder mappings:
            SyncConfig confInstance = Init.InitializeFolderMappings();
            
            // initialize source and destination files:
            Init.InitializeFiles(confInstance);

            

        }

        
    }
}