﻿using System;
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

                var filesDict = WorkingWithFiles.GetSourceAndDestFile(action.File1, action.File2);
                source = filesDict[FileType.Source] != null ? filesDict[FileType.Source].fullPath : "";
                dest = filesDict[FileType.Destination] != null ? filesDict[FileType.Destination].fullPath : "";

                writer.WriteLine(direction + action.ActionType + direction 
                    + source + "   -   " + dest);
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
            //int filesToDelete = syncExec.ActionsList.FindAll(x => x.ActionType == ActionType.Delete).Count;

            
            
            

            Console.WriteLine("Files to create:            " + filesToCreate);
            Console.WriteLine("Files to update:            " + filesToUpdate);
            Console.WriteLine("Files to rename and move:   " + filesToRenameMove);
            Console.WriteLine("Files to rename:            " + filesToRename);
            Console.WriteLine("Files to move:              " + filesToMove);
            //Console.WriteLine("Files to delete:            " + filesToDelete);
            
            Console.WriteLine("For further details, please, see the actions log file -\n"
                + syncExec.SyncConfig.ActionsPreviewLogFile);

            try
            {
                ShowSpaceInfo(syncExec);
            }
            catch(Exception e)
            {
                ErrorHandling.HandleException(syncExec.SyncConfig,e);
            }
        }

        private static void ShowSpaceInfo(SyncExecution syncExec)
        {
            string sourceVolume = DriveHelper.GetSourceVolume(syncExec.SyncConfig);
            string destVolume = DriveHelper.GetDestVolume(syncExec.SyncConfig);

            int sourceAvailableSpace;
            int destAvailableSpace;
            bool networkTransfer = false;

            if (sourceVolume.Contains(@"\\"))
            {
                sourceAvailableSpace = 0;
                networkTransfer = true;
            }
            else
            {
                sourceAvailableSpace = DriveHelper.GetVolumeAvailableSpace(sourceVolume);
            }
            if (destVolume.Contains(@"\\"))
            {
                destAvailableSpace = 0;
                networkTransfer = true;
            }
            else
            {
                destAvailableSpace = DriveHelper.GetVolumeAvailableSpace(destVolume);
            }

            if (!networkTransfer)
            {
                Console.WriteLine("\nRequired space on disk " + sourceVolume + ": " + syncExec.SpaceNeededInSource +
                                  "MB. Available space: "
                                  + sourceAvailableSpace + "MB");
                Console.WriteLine("Required space on disk " + destVolume + ": " + syncExec.SpaceNeededInDestination +
                                  "MB. Available space: "
                                  + destAvailableSpace + "MB");
                if (sourceAvailableSpace < syncExec.SpaceNeededInSource)
                    Console.WriteLine(
                        "WARNING: there is not enough available space for synchronization to complete. Disk " +
                        sourceVolume);
                if (destAvailableSpace < syncExec.SpaceNeededInDestination)
                    Console.WriteLine(
                        "WARNING: there is not enough available space for synchronization to complete. Disk " +
                        destVolume);
            }
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
                    var sourceFile = WorkingWithFiles.GetSourceAndDestFile(action.File1, action.File2)[FileType.Source];
                    var destFile = WorkingWithFiles.GetSourceAndDestFile(action.File1, action.File2)[FileType.Destination];
                    var sourcePath = sourceFile != null ? sourceFile.fullPath : "";
                    var destPath = destFile != null ? destFile.fullPath : "";

                    writer.WriteLine("\taction:       " + action.ActionType);
                    writer.WriteLine("\tdirection:    " + action.ActionDirection);
                    writer.WriteLine("\tsource file:  " + sourcePath);
                    writer.WriteLine("\tdest file:    " + destPath);
                    writer.WriteLine("\treason:       " + action.ExceptionMessage);
                    writer.WriteLine();
                }
            }
        }

        internal static void WriteFailedActionsToLog(SyncExecution syncExec)
        {
            using (var logWriter = File.AppendText(syncExec.SyncConfig.ErrorLogFile))
            {
                WriteFailedActions(syncExec, logWriter);
            }
        }
    }
}
