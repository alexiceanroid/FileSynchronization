using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileSynchronization;

namespace ConsoleInterface
{
    public static class Program
    {
        static void Main(string[] args)
        {
            
            Console.WriteLine("Starting the FileSync app... \n"
                + "If you want to stop its execution at any time, please, press CTRL+C");

            Console.CancelKeyPress += (sender, e) =>
            {

                Console.WriteLine("\n\n\n\n\nExiting...");
                if(Console.ReadKey() != null)
                    Environment.Exit(0);
            };

            // read and initialize source and destination folders:
            var confInstance = new SyncConfig();
            confInstance.InitializeFolderMappings();

            var syncExec = new SyncExecution(confInstance);
            syncExec = PrepareSyncExec(syncExec);

            syncExec.AppendActionList();

            //SyncHelper.DisplayActionsList(syncExec);
            SyncHelper.PreviewChanges(syncExec);
            string proceedWithSync = "";
            while (proceedWithSync != "yes" && proceedWithSync != "no")
            {
                Console.WriteLine("\nWould you like to perform synchronization (yes/no)?");
                proceedWithSync = Console.ReadLine();
                if (proceedWithSync == "yes")
                {
                    syncExec.PerformActions();
                    //SyncHelper.DisplayFailedActions(syncExec);

                    if (syncExec.AnyChangesNeeded)
                        CSVHelper.SaveFileMappingToCsv(syncExec);
                }
                else if (proceedWithSync == "no")
                {
                    Console.WriteLine("The synchronization has been cancelled, exiting the app");
                }
            }


            Console.WriteLine("Execution completed! Press any key to exit");
            if(Console.ReadKey() != null)
                Environment.Exit(0);

            
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

        
    }
}
