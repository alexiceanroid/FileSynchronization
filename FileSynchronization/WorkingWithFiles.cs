
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
                    var files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);
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

        public static void ArchiveFile(FileExtended lastFile, string log, string archiveFolder)
        {
            string logMessage = "";
            try
            {
                if (!Directory.Exists(archiveFolder))
                    Directory.CreateDirectory(archiveFolder);
                string newFilePath = archiveFolder + @"\" + lastFile.FileName;

                if (File.Exists(newFilePath))
                {
                    var fileInfo = new FileInfo(newFilePath);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    long existingFileSize = fileInfo.Length;
                    if (existingFileSize != lastFile.FileSize)
                    {
                        newFilePath = newFilePath.Replace(fileNameWithoutExtension,
                            fileNameWithoutExtension + "_2");
                        File.Move(lastFile.fullPath, newFilePath);
                        logMessage = "Archived file " + lastFile.FileName + " and changed its name to "
                                     + Path.GetFileName(newFilePath);
                    }
                    else
                    {
                        File.Delete(lastFile.fullPath);
                        logMessage = "Deleted file " + lastFile.FileName;
                    }
                }
                else
                {
                    File.Move(lastFile.fullPath, newFilePath);
                    logMessage = "Archived file " + lastFile.FileName;
                }

            }
            catch (Exception e)
            {
                logMessage = "Could not archive file " + lastFile.FileName
                             + ": \n" + e.Message;
            }
            using (var logWriter = File.AppendText(log))
            {
                string timestamp = DateTime.Now.ToShortDateString() + " "
                                   + DateTime.Now.ToLongTimeString();
                logWriter.WriteLine(timestamp + ": " + logMessage);
            }
        }
    }
}