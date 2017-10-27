using System;

namespace FileSynchronization
{
    public partial class SyncExecution
    {
        private void ActionCreate(FilePairAction action)
        {
            var sourceFile = action._file1.fileType == FileType.Source? action._file1 : action._file2;
            var destFile = action._file2.fileType == FileType.Destination ? action._file2 : action._file1;

            //switch(action.actionDirection)
        }

        
    }
}