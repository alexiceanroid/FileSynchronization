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
        private static FileSystemWatcher watcher;
        static void Main(string[] args)
        {
            // read and initialize source and destination folder mappings:
            SyncConfig confInstance = Init.InitializeFolderMappings();
            
            // initialize source and destination files:
            Init.InitializeFiles(confInstance);

            string source_path = @"C:\temp\source folder2\New Text Document2.txt";
            string dest_path = @"C:\temp\destination folder\New Text Document.txt";

            Console.WriteLine("source:      " + Kernel32.GetCustomFileId(source_path));
            Console.WriteLine("destination: " + Kernel32.GetCustomFileId(dest_path));
        }

        }

        
}
