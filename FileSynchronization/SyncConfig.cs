using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{


    public class SyncConfig
    {

        public HashSet<FileInfo> SourceFiles { get; set; }
        public Dictionary<string, string> FileMapping { get; set; }
        public HashSet<FileInfo> DestinationFiles { get; set; }
        public Dictionary<string, string> FolderMappings;

        public SyncConfig()
        {
            FolderMappings = new Dictionary<string, string>();
            SourceFiles = new HashSet<FileInfo>();
            DestinationFiles = new HashSet<FileInfo>();
            FileMapping = new Dictionary<string, string>();
        }

        public void SetFileMapping()
        {
            
        }
    }
}