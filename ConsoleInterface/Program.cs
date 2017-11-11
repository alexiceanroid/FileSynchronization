using System;
using System.Collections.Generic;
using System.Globalization;
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

            SyncConfig confInstance = null;
            try
            {
                Console.WriteLine("Initializing folder mappings...");
                confInstance = new SyncConfig();
                string LogFolder = confInstance.Parameters["LogFolder"];
                string MappingCsvFileName = confInstance.Parameters["FileMappingFile"];
                
                Console.WriteLine("Done");

                

                var syncExec = new SyncExecution(confInstance);

                

                // initialize source and destination files:
                Init.InitializeFiles(syncExec);

                // remove duplicates if this is configured:
                if (confInstance.Parameters["RemoveDuplicates"] == "yes")
                    DuplicatesHandling.ShowDuplicates(syncExec);

                // retrieve existing file mapping from CSV
                // and, if necessary, create additional mapping from paths
                Init.InitFileMapping(syncExec);

                syncExec.AppendActionListWithUpdateCreateMove();

                if (!syncExec.AnyChangesNeeded)
                {
                    if (!File.Exists(MappingCsvFileName))
                    {
                        CSVHelper.SaveFileMappingToCsv(syncExec);
                    }
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

                if (syncExec.FailedActions.Count > 0)
                    Console.WriteLine("\nSome actions failed: for details, see the error log file -\n"
                                      + syncExec.SyncConfig.ErrorLogFile);
                else
                {
                    if (proceedWithSync == "yes")
                        Console.WriteLine("Execution completed sucessfully.");
                }
            }
            catch (Exception ex)
            {
                ExitApp("Could not initialize folder mappings. \n" + ex.Message);
            }

            
            ExitApp("");
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

        static void ListFilesLists(SyncExecution syncExec)
        {
            Console.WriteLine("source files:");
            foreach (var s in syncExec.SourceFiles)
            {
                Console.WriteLine(s.fullPath);
                Console.WriteLine(s.fileID);
            }

            Console.WriteLine("\ndest files:");
            foreach (var d in syncExec.DestFiles)
            {
                Console.WriteLine(d.fullPath);
                Console.WriteLine(d.fileID);
            }
        }

        static void DisplayFileInfo(string path)
        {
            var fInfo = new FileInfo(path);
            string lastWriteDate = fInfo.LastAccessTimeUtc.ToString(CultureInfo.InvariantCulture);
            string fileId = Kernel32.GetCustomFileId(path);
            FileExtended f = new FileExtended(FileType.Source,Path.GetPathRoot(path),path,
                lastWriteDate, fileId);

            Console.WriteLine(f);
        }
    }
}