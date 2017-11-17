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

    public class RelativePathComparer : IComparer<FileExtended>
    {
        public int Compare(FileExtended f1, FileExtended f2)
        {
            return String.Compare(f1.RelativePath, f2.RelativePath,StringComparison.Ordinal);
        }
    }

    public class FileExtended : IEqualityComparer<FileExtended>, IComparable<FileExtended>, IEquatable<FileExtended>
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
                return null;
            }
        }

        public string FileName => Path.GetFileName(fullPath);
        public long FileSize => (new FileInfo(fullPath)).Length;
        public string FileNameAndSize => FileName +" - " + FileSize.ToString();

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
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FileExtended) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) fileType * 397) ^ (fileID != null ? fileID.GetHashCode() : 0);
            }
        }

        public int CompareTo(FileExtended otherFile)
        {
            int result;

            var otherNameAndSize = otherFile.FileNameAndSize;
            var otherPath = otherFile.fullPath;

            result = String.Compare(FileNameAndSize, otherNameAndSize, StringComparison.Ordinal);
            if (result == 0)
            {
                result = String.Compare(fullPath, otherPath, StringComparison.Ordinal);
            }

            return result;
        }

        public override string ToString()
        {
            return "File type:       " + fileType + "\n"
                   + "Base path:       " + basePath + "\n"
                   + "Last write date: " + lastWriteDateTime + "\n"
                   + "File Id:         " + fileID + "\n"
                   + "Full path:       " + fullPath + "\n"
                   + "File name:       " + FileName + "\n"
                   + "File size:       " + FileSize;
        }

        public bool Equals(FileExtended other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other)) return true;
            return fileType == other.fileType && string.Equals(fileID, other.fileID);
        }
    }
    
}