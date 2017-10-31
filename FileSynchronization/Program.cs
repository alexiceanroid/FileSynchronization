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
using FileInfo = System.IO.FileInfo;

namespace FileSynchronization
{
    class Program
    {
        static long startDateTicks = new DateTime(2000,1,1,0,0,0).Ticks;
        static void Main(string[] args)
        {
            // read and initialize source and destination folders:
            SyncConfig confInstance = Init.InitializeFolderMappings();

            var syncExec = new SyncExecution(confInstance);
            syncExec = PrepareSyncExec(syncExec);

            syncExec.AppendActionList();

            syncExec.DisplayActionsList();

            syncExec.PerformActions();

            CSVHelper.SaveFileMappingToCsv(syncExec);

        }

        

        static SyncExecution PrepareSyncExec(SyncExecution syncExec)
        {
            // initialize source and destination files:
            Init.InitializeFiles(syncExec);

            // retrieve existing file mapping from CSV
            // and, if necessary, create additional mapping from paths
            Init.InitFileMapping(syncExec);

            return syncExec;
        }

        static void AllFilesTest()
        {
            var allFilesPaths = Directory.GetFiles(@"E:\synced_D", "*.*", SearchOption.AllDirectories);
            var allFilesDict = new Dictionary<string,string>();

            foreach (var filePath in allFilesPaths)
            {
                //var file = new FileInfo(filePath);
                //var fileExtension = file.Extension;
                //int fileCreateDateTicks = (int)(file.CreationTimeUtc.Ticks - startDateTicks);
                var filId = Kernel32.GetCustomFileId(filePath);
                //long newTicks = file.CreationTimeUtc.Ticks;

                allFilesDict.Add(filId, filePath);
            }
            /*
            var minDate = allFilesDict.Values.Min();
            var maxDate = allFilesDict.Values.Max();

            var fileMinDate = allFilesDict.FirstOrDefault(x => x.Value == minDate).Key;
            var fileMaxDate = allFilesDict.FirstOrDefault(x => x.Value == maxDate).Key;

            Console.WriteLine($"Min date: {minDate}. The file: {fileMinDate}");
            Console.WriteLine($"Max date: {maxDate}. The file: {fileMaxDate}");
            */
            
            var troublesomeFiles = allFilesDict.GroupBy(x => x.Value).Where(grp => grp.Count() > 1).Select(grp => grp.Key).ToList();
            foreach (var fileName in troublesomeFiles)
            {
                Console.WriteLine(fileName);
            }
            

            
        }
    }
}
