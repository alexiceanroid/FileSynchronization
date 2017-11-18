
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

        public static void ArchiveFile(FileExtended fileToArchive, string log, string archiveFolder, string reason)
        {
            string logMessage = "";
            try
            {
                
                string newFilePath = archiveFolder + @"\" + fileToArchive.fullPath.Replace(":","");

                string dir = Path.GetDirectoryName(newFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (File.Exists(newFilePath))
                {
                    var fileInfo = new FileInfo(newFilePath);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    long existingFileSize = fileInfo.Length;
                    if (existingFileSize != fileToArchive.FileSize)
                    {
                        newFilePath = newFilePath.Replace(fileNameWithoutExtension,
                            fileNameWithoutExtension + "_2");
                        File.Move(fileToArchive.fullPath, newFilePath);
                        logMessage = "Archived file " + fileToArchive.fullPath + "(size " + fileToArchive.FileSize/(1024)
                            + "KB) and changed its name to " + Path.GetFileName(newFilePath);
                    }
                    else
                    {
                        File.Delete(fileToArchive.fullPath);
                        logMessage = "Deleted file " + fileToArchive.fullPath + "(size " + fileToArchive.FileSize / (1024)
                            +"KB)";
                    }
                }
                else
                {
                    string fullPath = fileToArchive.fullPath;
                    int sizeKb = (int)Math.Floor((double)fileToArchive.FileSize / 1024);
                    File.Move(fileToArchive.fullPath, newFilePath);
                    logMessage = reason + ": archived file " + fullPath
                                 + " (size " + sizeKb + "KB)";
                }

            }
            catch (Exception e)
            {
                logMessage = "Could not archive file " + fileToArchive.FileName
                             + ": \n" + e.Message;
            }
            using (var logWriter = File.AppendText(log))
            {
                string timestamp = DateTime.Now.ToShortDateString() + " "
                                   + DateTime.Now.ToLongTimeString();
                logWriter.WriteLine(timestamp + ": " + logMessage);
            }
        }

        public static Dictionary<FileType, FileExtended> GetSourceAndDestFile(FileExtended file1, FileExtended file2)
        {
            var files = new Dictionary<FileType, FileExtended>();
            FileExtended sourceFile;
            FileExtended destFile;

            if (file1 == null)
            {
                sourceFile = file2.fileType == FileType.Source ? file2 : null;
                destFile = file2.fileType == FileType.Destination ? file2 : null;
            }
            else if (file2 == null)
            {
                sourceFile = file1.fileType == FileType.Source ? file1 : null;
                destFile = file1.fileType == FileType.Destination ? file1 : null;
            }
            else
            {
                sourceFile = file1.fileType == FileType.Source ? file1 : file2;
                destFile = file2.fileType == FileType.Destination ? file2 : file1;
            }
            files.Add(FileType.Source, sourceFile);
            files.Add(FileType.Destination, destFile);

            return files;
        }
    }
}