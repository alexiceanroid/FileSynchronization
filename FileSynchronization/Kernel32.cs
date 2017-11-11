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
            if (File.Exists(path))
            {
                using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (!GetFileInformationByHandle(file.SafeFileHandle, out info))
                    {
                        throw new Win32Exception();
                    }
                }
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        //public static UInt64 GetFileIndex(string path)
        //{
        //    BY_HANDLE_FILE_INFORMATION info;
        //    GetFileInformation(path, out info);
        //    return info.FileIndexLow | ((UInt64)info.FileIndexHigh << 32);
        //}

        //public static Tuple<uint, ulong> GetFileIdentifier(string path)
        //{
        //    BY_HANDLE_FILE_INFORMATION info;
        //    GetFileInformation(path, out info);
        //    return new Tuple<uint, ulong>(info.VolumeSerialNumber,
        //        info.FileIndexLow | ((UInt64)info.FileIndexHigh << 32));
        //}

        public static string GetCustomFileId(string path)
        {
            // this method returns file ID that stays constant after file renaming, changing, moving within a single disk volume
            // however, this file ID changes if file gets relocated to a different volume and does not regain a previous 
            // value if returned back to original volume!!
            BY_HANDLE_FILE_INFORMATION info;
            GetFileInformation(path, out info);
            string driveLetter = Path.GetPathRoot(path);
            return driveLetter  + info.FileIndexLow.ToString() + "-"
                + info.FileIndexHigh.ToString();
        }


    }
}