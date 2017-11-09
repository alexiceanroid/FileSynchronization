
using System;
using System.Collections.Generic;
using System.IO;

namespace FileSynchronization
{
    public static class WorkingWithFiles
    {
        public static void GetFiles(string directory, List<string> filesList)
        {
            try
            {
                if (directory.ToString().Contains("\\$RECYCLE.BIN\\"))
                    return;

                var subFolders = Directory.GetDirectories(directory);
                foreach (var subFolder in subFolders)
                {
                    GetFiles(subFolder, filesList);
                }


                try
                {
                    var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                    filesList.AddRange(files);
                }
                catch (UnauthorizedAccessException e)
                {
                    //Console.WriteLine("Access to directory " + directory 
                    //    + " is denied, skipping it...");
                }
            }
            catch (Exception e)
            {
                // ignored
            }
        }
    }
}