using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public class GoodSync
    {
        private string _resultStatus;
        public string ResultStatus
        {
            get { return _resultStatus; }
            set
            {
                if ((value == "error") || (value == "success"))
                {
                    _resultStatus = value;
                }
            }
        }

        public string ResultInfo { get; set; }

        public Dictionary<string, string> folderMappings;

        public GoodSync()
        {
            ResultStatus = "success";
            folderMappings = new Dictionary<string, string>();
        }
    }
}
