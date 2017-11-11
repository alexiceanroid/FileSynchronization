using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public static class DuplicatesHandling
    {
        public static void ShowDuplicates(SyncExecution syncExec)
        {
            var sourceFiles = new List<FileExtended>(syncExec.SourceFiles);
            var destFiles = new List<FileExtended>(syncExec.DestFiles);

            sourceFiles.Sort();
            destFiles.Sort();

            var duplSourceFiles = sourceFiles
                .GroupBy(f => f.FileName)
                .Where(grp => grp.Count() > 1)
                .Select(grp => grp.Key).ToList();
            var duplDestFiles = destFiles
                .GroupBy(f => f.FileName)
                .Where(grp => grp.Count() > 1)
                .Select(grp => grp.Key).ToList();

            duplSourceFiles.Sort();
            duplDestFiles.Sort();

            Console.WriteLine("\nDuplicate source files:");
            foreach (var s in duplSourceFiles)
            {
                Console.WriteLine(s + "\n");
            }

            Console.WriteLine("\nDuplicate dest files:");
            foreach (var d in duplDestFiles)
            {
                Console.WriteLine(d + "\n");
            }
        }

        
    }
}
