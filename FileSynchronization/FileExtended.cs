using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FileSynchronization
{
    public enum FileType
    {
        Source,
        Destination
    }



    public class FileExtended : IEqualityComparer<FileExtended>
    {
        public readonly FileType fileType;
        public readonly string basePath;
        public readonly string fullPath;
        public readonly string lastWriteDateTime;
        public string fileID;

        public FileExtended(FileType _fileType, string _basePath, 
            string _fullPath, string _lastWriteDate, string _fileId)
        {
            fileType = _fileType;
            basePath = _basePath;
            fullPath = _fullPath;
            lastWriteDateTime = _lastWriteDate;
            fileID = _fileId;
        }

        public FileExtended()
        {
        }

        public FileExtended(FileType _fileType, string _basePath,
            string _fullPath, string _fileId)
        {
            fileType = _fileType;
            basePath = _basePath;
            fullPath = _fullPath;
            lastWriteDateTime = File.Exists(_fullPath) ?
            (new FileInfo(_fullPath)).LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)
                :
                null;
            fileID = _fileId;
        }


        public string RelativePath
        {
            get
            {
                if (!String.IsNullOrEmpty(fileID))
                {
                    return fullPath.Replace(basePath, "");
                }
                else
                {
                    return null;
                }
            }
        }

        public string FileName => Path.GetFileName(fullPath);
        public long FileSize => (new FileInfo(fullPath)).Length;
        public string FileNameAndSize => FileName + FileSize.ToString();

        public bool Equals(FileExtended x, FileExtended y)
        {
            return x.GetHashCode() == y.GetHashCode();
        }

        public int GetHashCode(FileExtended obj)
        {
            return fileID.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            bool res = false;
            if (obj is FileExtended f)
            {
                res = this.GetHashCode() == f.GetHashCode();
            }
            return res;
        }

        public override int GetHashCode()
        {
            return fileID.GetHashCode();
        }
    }
    
}