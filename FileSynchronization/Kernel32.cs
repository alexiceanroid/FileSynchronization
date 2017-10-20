using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace FileSynchronization
{
    public static class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(
            Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation
        );

        public static void GetFileInformation(string path, out BY_HANDLE_FILE_INFORMATION info)
        {
            using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (!GetFileInformationByHandle(file.SafeFileHandle, out info))
                {
                    throw new Win32Exception();
                }
            }
        }

        public static UInt64 GetFileIndex(string path)
        {
            BY_HANDLE_FILE_INFORMATION info;
            GetFileInformation(path, out info);
            return info.FileIndexLow | ((UInt64)info.FileIndexHigh << 32);
        }

        public static Tuple<uint, ulong> GetFileIdentifier(string path)
        {
            BY_HANDLE_FILE_INFORMATION info;
            GetFileInformation(path, out info);
            return new Tuple<uint, ulong>(info.VolumeSerialNumber,
                info.FileIndexLow | ((UInt64)info.FileIndexHigh << 32));
        }

        public static string GetCustomFileId(string path)
        {
            BY_HANDLE_FILE_INFORMATION info;
            GetFileInformation(path, out info);
            return info.FileIndexLow.ToString() + "-" + info.FileIndexHigh.ToString();
        }

        public static void ShowFileInfo(string path)
        {
            BY_HANDLE_FILE_INFORMATION info;
            GetFileInformation(path, out info);
            Console.WriteLine("Volume serial number: " + info.VolumeSerialNumber);
            Console.WriteLine("NumberOfLinks: " + info.NumberOfLinks);
            Console.WriteLine("FileIndexHigh: " + info.FileIndexHigh);
            Console.WriteLine("FileIndexLow: " + info.FileIndexLow + "\n");
        }
    }


}