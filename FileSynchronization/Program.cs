using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;

namespace FileSynchronization
{
    class Program
    {
        static void Main(string[] args)
        {
            var configReader = new AppSettingsReader();
            object configLocation = configReader.GetValue("ConfigFile", typeof(string));
            //Type configType = typeof(configLocation.GetType());
            Type t = configLocation.GetType();
        }
    }

    
}
