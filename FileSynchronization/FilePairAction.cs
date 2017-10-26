using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public class FilePairAction
    {
        public FileExtended _file1;
        public FileExtended? _file2;
        public ActionType actionType { get; set; }
        public Direction actionDirection { get; set; }

        public FilePairAction(FileExtended file1, FileExtended? file2)
        {
            this._file1 = file1;
            this._file2 = file2;
        }
    }

    

    public enum ActionType
    {
        None,
        Create, 
        Rename,
        Move,
        Update,
        Delete
    }

    public enum Direction
    {
        None,
        SourceToDestination,
        DestinationToSource
    }
}
