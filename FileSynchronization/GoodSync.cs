using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public enum ResultStatusEnum
    {
        Success,
        Error
    };

    public class GoodSync
    {

        public ResultStatusEnum ResultStatus { get; set; }

        public string ResultInfo { get; set; }
        public HashSet<FileInfo> SourceFiles { get; set; }
        public HashSet<FileInfo> SourceFilesToProcess { get; set; }
        public HashSet<FileInfo> DestinationFiles { get; set; }

        public Dictionary<string, string> folderMappings;

        public GoodSync()
        {
            ResultStatus = ResultStatusEnum.Success;
            folderMappings = new Dictionary<string, string>();
            SourceFiles = new HashSet<FileInfo>();
            SourceFilesToProcess = new HashSet<FileInfo>();
            DestinationFiles = new HashSet<FileInfo>();
        }

        //public GoodSync(int mappingsCount)
        //{
        //    ResultStatus = ResultStatusEnum.Success;
        //    folderMappings = new Dictionary<string, string>(mappingsCount);
        //}
    }
}
