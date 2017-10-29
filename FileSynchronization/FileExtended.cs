using System;
using System.Collections;
using System.Collections.Generic;

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



            public override int GetHashCode()
            {
                return this.fileID.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is FileExtended externalFile)
                {
                    return this.GetHashCode() == externalFile.GetHashCode();
                }
                else
                {
                    return false;
                }
            }

            public bool Equals(FileExtended file1, FileExtended file2)
            {
                var file1Hash = GetHashCode(file1);
                var file2Hash = GetHashCode(file2);

                return file1Hash == file2Hash;
            }

            public int GetHashCode(FileExtended file)
            {
                return file.fileID.GetHashCode();
            }
        }
    
}