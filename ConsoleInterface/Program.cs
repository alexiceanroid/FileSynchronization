using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
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
                if (Console.ReadKey() != null)
                    Environment.Exit(0);
            };

            // read and initialize source and destination folders:
            var confInstance = new SyncConfig();
            try
            {

                confInstance.InitializeFolderMappings();
            }
            catch (Exception ex)
            {
                ExitApp("Could not initialize folder mappings. \n" + ex.Message);
            }

            var syncExec = new SyncExecution(confInstance);
            syncExec = PrepareSyncExec(syncExec);

            syncExec.AppendActionList();

            if (!syncExec.AnyChangesNeeded)
            {
                ExitApp("No changes detected");
            }

            SyncHelper.WriteActionsListToLog(syncExec);
            SyncHelper.PreviewChangesStats(syncExec);
            string proceedWithSync = "";
            while (proceedWithSync != "yes" && proceedWithSync != "no")
            {
                Console.WriteLine("\nWould you like to perform synchronization (yes/no)?");
                proceedWithSync = Console.ReadLine();
                if (proceedWithSync == "yes")
                {
                    syncExec.PerformActions();
                    SyncHelper.WriteFailedActionsToLog(syncExec);

                    if (syncExec.AnyChangesNeeded)
                        CSVHelper.SaveFileMappingToCsv(syncExec);
                }
                else if (proceedWithSync == "no")
                {
                    Console.WriteLine("The synchronization has been cancelled, exiting the app");
                    Thread.Sleep(1500);
                    Environment.Exit(0);
                }
            }

            if(syncExec.FailedActions.Count > 0)
                Console.WriteLine("\nSome actions failed: for details, see the error log file -\n" 
                    + syncExec.SyncConfig.ErrorLogFile);
            else
            {
                if(proceedWithSync == "yes")
                    Console.WriteLine("Execution completed sucessfully.");
            }


            
            

            
            ExitApp("");
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

        static void ExitApp(string reason)
        {
            Console.WriteLine("\n" + reason);
            Console.WriteLine("\nPress any key to exit");
            if (Console.ReadKey() != null)
                Environment.Exit(0);
        }

        static void FolderCleanup()
        {
            string folder = @"A:\$RECYCLE.BIN";
            Directory.SetAccessControl(folder, new DirectorySecurity("test", AccessControlSections.Owner));
            Directory.Delete(folder, true);
        }
    }
}