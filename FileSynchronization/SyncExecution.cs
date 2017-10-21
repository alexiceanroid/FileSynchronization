using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public class SyncExecution
    {
        private List<FilePairAction> _actionList;
        private SyncConfig _syncConfig;

        public SyncExecution(SyncConfig _syncConfig)
        {
            this._syncConfig = _syncConfig;
        }

        public void PopulateActionList()
        {
            _actionList = new List<FilePairAction>();
            var fileMapping = _syncConfig.FileMapping;

            foreach (var filePair in fileMapping)
            {
                var file1 = filePair.Key;
                var file2 = filePair.Value;
                FilePairAction filePairAction = new FilePairAction(file1, file2);

                if (file2 == null)
                {
                    filePairAction.actionType = ActionType.Create;
                    if (file1.FileType == FileType.Source)
                    {
                        filePairAction.actionDirection = Direction.SourceToDestination;
                    }
                    else
                    {
                        filePairAction.actionDirection = Direction.DestinationToSource;
                    }
                }
            }
        }
    }
}