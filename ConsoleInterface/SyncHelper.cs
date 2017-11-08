using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FileSynchronization;

namespace ConsoleInterface
{
    static class SyncHelper
    {
        internal static void WriteActionsList(SyncExecution syncExec, StreamWriter writer)
        {
            var actionList = syncExec.ActionsList.Where(x => x.ActionType != ActionType.None);
            string direction;
            string source;
            string dest;


            writer.WriteLine("\nList of actions to perform: ");
            foreach (var action in actionList)
            {
                switch (action.ActionDirection)
                {
                    case Direction.None:
                        direction = "==";
                        break;
                    case Direction.SourceToDestination:
                        direction = "=>";
                        break;
                    case Direction.DestinationToSource:
                        direction = "<=";
                        break;
                    case Direction.Unknown:
                        direction = "??";
                        break;
                    default:
                        throw new Exception("Ivalid direction");
                }

                var filesDict = syncExec.GetSourceAndDestFile(action.File1, action.File2);
                source = filesDict[FileType.Source] != null ? filesDict[FileType.Source].fullPath : "";
                dest = filesDict[FileType.Destination] != null ? filesDict[FileType.Destination].fullPath : "";

                writer.WriteLine(source + "\n" +
                                  direction + " " + action.ActionType + " " + direction + 
                                  "\n" + dest);
            }
        }

        internal static void WriteActionsListToLog(SyncExecution syncExec)
        {
            string logFile = syncExec.SyncConfig.ActionsPreviewLogFile;
            using (var writer = new StreamWriter(logFile))
            {
                WriteActionsList(syncExec, writer);
            }
        }

        internal static void PreviewChangesStats(SyncExecution syncExec)
        {
            Console.WriteLine("\n");
            int filesToCreate = syncExec.ActionsList.FindAll(x => x.ActionType == ActionType.Create).Count;
            int filesToUpdate = syncExec.ActionsList.FindAll(x => x.ActionType == ActionType.Update).Count;
            int filesToRenameMove = syncExec.ActionsList.FindAll(x => x.ActionType == ActionType.RenameMove).Count;
            int filesToRename = syncExec.ActionsList.FindAll(x => x.ActionType == ActionType.Rename).Count;
            int filesToMove = syncExec.ActionsList.FindAll(x => x.ActionType == ActionType.Move).Count;
            int filesToDelete = syncExec.ActionsList.FindAll(x => x.ActionType == ActionType.Delete).Count;

            Console.WriteLine("Files to create:            " + filesToCreate);
            Console.WriteLine("Files to update:            " + filesToUpdate);
            Console.WriteLine("Files to rename and move:   " + filesToRenameMove);
            Console.WriteLine("Files to rename:            " + filesToRename);
            Console.WriteLine("Files to move:              " + filesToMove);
            Console.WriteLine("Files to delete:            " + filesToDelete);
            Console.WriteLine("For further details, please, see the actions log file -\n"
                + syncExec.SyncConfig.ActionsPreviewLogFile);
        }

        internal static void WriteFailedActions(SyncExecution syncExec, StreamWriter writer)
        {
            if (syncExec.FailedActions.Count > 0)
            {
                var failedActions = new List<FilePairAction>(syncExec.FailedActions);
                failedActions.Sort();
                writer.WriteLine("\n");
                writer.WriteLine(failedActions.Count + 
                    " actions could not be performed:");
                foreach (var action in failedActions)
                {
                    writer.WriteLine("\taction:       " + action.ActionType);
                    writer.WriteLine("\tdirection:    " + action.ActionDirection);
                    writer.WriteLine("\tsource file:  " + action.File1.fullPath);
                    writer.WriteLine("\tdest file:    " + action.File2.fullPath);
                    writer.WriteLine("\treason:       " + action.ExceptionMessage);
                    writer.WriteLine();
                }
            }
        }

        internal static void WriteFailedActionsToLog(SyncExecution syncExec)
        {
            using (var logWriter = new StreamWriter(syncExec.SyncConfig.ErrorLogFile))
            {
                WriteFailedActions(syncExec, logWriter);
            }
        }
    }
}
