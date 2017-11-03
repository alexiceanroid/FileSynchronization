using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public class FilePairAction
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
