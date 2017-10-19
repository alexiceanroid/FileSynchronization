using System.Collections.Generic;
using System.IO;

namespace FileWatcher
{
    public enum ResultStatusEnum
    {
        Success,
        Error
    }

    

    public class GoodSync
    {

        public ResultStatusEnum ResultStatus { get; set; }

        public string ResultInfo { get; set; }
        public HashSet<FileInfo> SourceFiles { get; set; }
        public Dictionary<string, string> FileMapping { get; set; }
        public Dictionary<Dictionary<string,string>, string> FileMappingActions { get; set; }
        public HashSet<FileInfo> DestinationFiles { get; set; }

        public Dictionary<string, string> FolderMappings;

        public GoodSync()
        {
            ResultStatus = ResultStatusEnum.Success;
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
