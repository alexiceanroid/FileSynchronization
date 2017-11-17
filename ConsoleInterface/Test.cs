using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using FileSynchronization;

namespace ConsoleInterface
{
    static class Test
    {
        static void FolderCleanup()
        {
            string folder = @"A:\$RECYCLE.BIN";
            Directory.SetAccessControl(folder, new DirectorySecurity("test", AccessControlSections.Owner));
            Directory.Delete(folder, true);
        }

        static void ListFilesLists(SyncExecution syncExec)
        {
            Console.WriteLine("source files:");
            foreach (var s in syncExec.SourceFiles.Values)
            {
                Console.WriteLine(s.fullPath);
                Console.WriteLine(s.fileID);
            }

            Console.WriteLine("\ndest files:");
            foreach (var d in syncExec.DestFiles.Values)
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
            FileExtended f = new FileExtended(FileType.Source, Path.GetPathRoot(path), path,
                lastWriteDate, fileId);

            Console.WriteLine(f);
        }

        static void TestSpaceSizes()
        {
            string disk = @"C:\";
            int x = DriveHelper.GetVolumeAvailableSpace(disk);
            Console.WriteLine("Available space on " + disk + ": " + x + "MB");
        }
    }
}
