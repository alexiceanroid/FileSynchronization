using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FileSynchronization;

namespace ConsoleInterface
{
    static class SyncHelper
    {
        internal static void DisplayActionsList(SyncExecution syncExec)
        {
            var actionList = syncExec.ActionsList;
            string direction;
            string source;
            string dest;


            Console.WriteLine("\nList of actions to perform: ");
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

                Console.WriteLine(source + "\n" +
                                  direction + " " + action.ActionType + " " + direction + 
                                  "\n" + dest);
            }
        }

        internal static void PreviewChanges(SyncExecution syncExec)
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
        }

        internal static void DisplayFailedActions(SyncExecution syncExec)
        {
            if (syncExec.FailedActions.Count > 0)
            {
                var failedActions = new List<FilePairAction>(syncExec.FailedActions);
                failedActions.Sort();
                Console.WriteLine("\n");
                Console.WriteLine(failedActions.Count + 
                    " actions could not be performed:");
                foreach (var action in failedActions)
                {
                    Console.WriteLine("\taction: " + action.ActionType);
                    Console.WriteLine("\tfile1:  " + action.File1.fullPath);
                    Console.WriteLine("\tfile2:  " + action.File2.fullPath);
                    Console.WriteLine("\treason: " + action.ExceptionMessage);
                    Console.WriteLine();
                }
            }
        }
    }
}
