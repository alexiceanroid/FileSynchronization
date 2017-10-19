using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSynchronization
{
    internal class FileWatcher
    {
        public FileSystemWatcher Watcher { get; set; }

        private void Watch(string path)
        {
            Watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                   | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.*"
            };
            Watcher.Changed += OnChanged;
            Watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            uint iterationNum = 0;
            uint iterationLim = 10;
            int pauseTime = 1;
            bool isFileReadyFlag = false;

            while (iterationNum <= iterationLim || isFileReadyFlag)
            {
                iterationNum++;
                isFileReadyFlag = IsFileReady(e.Name);
                Thread.Sleep(1000 * pauseTime);
            }
            Console.WriteLine("file changed!");
        }

        public static bool IsFileReady(String sFilename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return inputStream.Length > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}