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
        public static string ResolvePath(SyncConfig confInstance, string folderPath)
        {
            string resPath = null;
            string volLetter;
            try
            {

                int volLabelendInd = folderPath.IndexOfAny(new char[] {':', '\\'});

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

                    resPath = folderPath.Replace(volLabel, volLetter.Substring(0, 1));
                }
                else
                {
                    resPath = folderPath;
                }

                if (!Directory.Exists(resPath))
                {
                    throw new DirectoryNotFoundException();
                }
            }
            catch (Exception e)
            {
                var error = new AppError(DateTime.Now, e.TargetSite.ToString(), 
                    "resolving path " + folderPath, e.Message);
                ErrorHandling.WriteErrorLog(confInstance,error);
                throw e;
            }
            

            return resPath;
            
        }

        public static string GetSourceVolume(SyncConfig confInstance)
        {
            return Path.GetPathRoot(confInstance.FolderMappings.Keys.FirstOrDefault());
        }

        public static string GetDestVolume(SyncConfig confInstance)
        {
            return Path.GetPathRoot(confInstance.FolderMappings.Values.FirstOrDefault());
        }

        public static int GetVolumeAvailableSpace(string vol)
        {
            var volumeInfo = new DriveInfo(vol);

            return (int)volumeInfo.AvailableFreeSpace / (1024 * 1024); // MB
        }
    }
}
