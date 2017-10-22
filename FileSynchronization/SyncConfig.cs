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

        public List<FileExtended> SourceFiles { get; set; }
        public List<FileExtended> DestinationFiles { get; set; }
        public Dictionary<FileExtended, FileExtended> FileMapping { get; set; }
        
        public Dictionary<string, string> FolderMappings;

        public SyncConfig()
        {
            FolderMappings = new Dictionary<string, string>();
            SourceFiles = new List<FileExtended>();
            DestinationFiles = new List<FileExtended>();
            FileMapping = new Dictionary<FileExtended, FileExtended>();
        }

        
        
        
        
    }

    public class FileExtended
    {
        public FileType FileType { get; set; }
        public string BasePath { get; set; }
        public FileInfo FileInfo { get; set; }
        public string FileID { get; set; }
        public override int GetHashCode()
        {
            return this.FileID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is FileExtended externalFile)
            {
                return this.GetHashCode() == externalFile.GetHashCode();
            }
            else
            {
                return false;
            }
        }
    }

    public enum FileType
    {
        Source,
        Destination
    }
}