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

            //syncExec.DisplayActionsList();

            syncExec.PerformActions();

            if(syncExec.AnyChangesNeeded)
                CSVHelper.SaveFileMappingToCsv(syncExec);
            
            //ConsoleTest();
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

        private static void ConsoleTest()
        {
            Console.Write("\rfiles created: " + 1 + ";\n"
                          + "files updated: " + 2 + ";\n"
                          + "files renamed: " + 3 + ";\n"
                          + "files moved:   " + 4 + ";\n"
                          + "files deleted: " + 5);

            Console.SetCursorPosition(0, Console.CursorTop - 4);

            Console.Write("\rfiles created: " + 6 + ";\n"
                          + "files updated: " + 7 + ";\n"
                          + "files renamed: " + 8 + ";\n"
                          + "files moved:   " + 9 + ";\n"
                          + "files deleted: " + 10);
        }
    }
}
