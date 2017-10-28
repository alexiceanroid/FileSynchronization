using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public static class DirectoryFilesProcessing
    {
        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        /*
        public static void ProcessDirectory(string targetDirectory, List<FileExtended> fileInfos, string basePath, FileType fileType)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName, fileInfos, basePath, fileType);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory, fileInfos, basePath, fileType);
        }


        public static void ProcessFile(string path, List<FileExtended> fileInfos, string basePath, FileType fileType)
        {
            var filePath = String.Copy(path);
            var fileExtended = new FileExtended(
                fileType,
                basePath,
                (new FileInfo(filePath)).LastWriteTime.ToString(CultureInfo.InvariantCulture),
                Kernel32.GetCustomFileId(filePath)
            );

            fileInfos.Add(fileExtended);
            //_filesProcessed++;
            //Console.Write($"\r processed {_filesProcessed} files    ");
        }
        */
        
    }
}
