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

            string MappingCsvFileName = "";
            SyncConfig confInstance = null;
            try
            {
                Console.WriteLine("Initializing configuration...");
                confInstance = new SyncConfig();
                string LogFolder = confInstance.Parameters["LogFolder"];
                MappingCsvFileName = confInstance.Parameters["FileMappingFile"];
                foreach (var entry in confInstance.FolderMappings)
                {
                    Console.WriteLine(entry.Key + "  <=>  " + entry.Value);
                }
                Console.WriteLine("Done");
            }
            
            catch (Exception ex)
            {
                string mes = ex.Message;
                ExitApp("Could not initialize configuration. Check error log file for details \n" + mes);
            }


            var syncExec = new SyncExecution(confInstance);

            // initialize source and destination files:
            try
            {
                Init.InitializeFiles(syncExec);
            }
            catch (Exception e)
            {
                ExitApp("Could not initialize files. \n" + e.Message);

            }

            // remove duplicates if this is configured:
            try
            {
                if (confInstance.Parameters["RemoveDuplicates"] == "yes")
                    DuplicatesHandling.RemoveDuplicates(syncExec);
            }
            catch (Exception e)
            {
                ExitApp("Could not remove duplicates. \n" + e.Message);
            }


            try
            {
                // retrieve existing file mapping from CSV
                // and, if necessary, create additional mapping from paths
                Init.MapFiles(syncExec);
            }
            catch (Exception e)
            {
                ExitApp("Could not map files. \n" + e.Message);
            }

            try
            {
                syncExec.AppendActionListWithUpdateCreateMove();
            }
            catch (Exception e)
            {
                ExitApp("Could not complete the stage of identifying updates, creations and moves. \n" + e.Message);
            }
            

            

            if (syncExec.AnyChangesNeeded)
            {
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
                        Console.WriteLine("\n\nExecution completed sucessfully.");
                }

                if (!syncExec.AnyChangesNeeded)
                {
                    if (!File.Exists(MappingCsvFileName))
                    {
                        CSVHelper.SaveFileMappingToCsv(syncExec);
                    }
                }

                ExitApp("");
            }
            else
            {
                ExitApp("No changes needed");
            }


            
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