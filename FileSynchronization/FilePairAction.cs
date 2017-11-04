using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public class FilePairAction : IComparable<FilePairAction>
    {
        public FileExtended File1;
        public FileExtended File2;
        public ActionType ActionType { get; set; }
        public Direction ActionDirection { get; set; }
        public bool? SyncSuccess { get; set; }
        public string ExceptionMessage { get; set; }

        public FilePairAction(FileExtended file1, FileExtended file2)
        {
            this.File1 = file1;
            this.File2 = file2;
        }

        public int CompareTo(FilePairAction other)
        {
            int res = 0;

            if (ActionType > other.ActionType)
            {
                res = 1;
            }
            else if (ActionType < other.ActionType)
            {
                res = -1;
            }
            else
            {
                var temp = File1.RelativePath.CompareTo(other.File1.RelativePath);
                if (temp > 0)
                    res = 1;
                else if (temp < 0)
                    res = -1;
                else
                {
                    res = 0;
                }
            }

            return res;
        }
    }

    

    public enum ActionType
    {
        None,
        Create, 
        Rename,
        Move,
        RenameMove,
        Update,
        Delete
    }

    public enum Direction
    {
        None,
        SourceToDestination,
        DestinationToSource,
        Unknown
    }
}
