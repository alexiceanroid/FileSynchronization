using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public static class DriveHelper
    {
        public static string ResolvePath(string folderPath)
        {
            string resPath;
            string volLetter;
            int volLabelendInd = folderPath.IndexOfAny(new char[] {':','\\'});

            // if volLabelendInd == 1 it means that volume label is not used 
            // and the input path should be returned 'as-is'
            if (volLabelendInd > 1)
            {
                string volLabel = folderPath.Substring(0, volLabelendInd);



                DriveInfo[] allDrives = DriveInfo.GetDrives();
                try
                {
                    volLetter = allDrives.FirstOrDefault(x => x.VolumeLabel == volLabel).Name;
                }
                catch (NullReferenceException ex)
                {
                    return null;
                }

                resPath = folderPath.Replace(volLabel, volLetter.Substring(0,1));
            }
            else
            {
                resPath = folderPath;
            }
            return resPath;
        }
    }
}
