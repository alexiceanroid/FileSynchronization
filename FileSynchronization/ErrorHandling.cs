using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronization
{
    public struct AppError
    {
        public DateTime DateTime;
        public string Method;
        public string Entry;
        public string ErrorMessage;

        public AppError(DateTime timeStamp, string method,
            string entry, string errorMessage)
        {
            DateTime = timeStamp;
            Method = method;
            Entry = entry;
            ErrorMessage = errorMessage;
        }
    }

    public static class ErrorHandling
    {
        private const string spaceSeparator = "       ";
        public static void WriteErrorLog(SyncConfig confInstance, AppError error)
        {
            string errorLogLocation = confInstance.LogFolder;
            string errorLogName = confInstance.ErrorLogFile;

            if (!Directory.Exists(errorLogLocation))
            {
                Directory.CreateDirectory(errorLogLocation);
            }

            using (StreamWriter sw = File.CreateText(errorLogName))
            {
                sw.WriteLine("Logging started on " + DateTime.Now.
                    ToString(CultureInfo.InvariantCulture));
                sw.WriteLine("\n\n");
            }

            // construct log text line
            string timeStamp = error.DateTime.ToShortDateString() + error.DateTime.ToLongTimeString();
            string logTextLine = timeStamp + spaceSeparator
                                 + error.Method + spaceSeparator
                                 + error.Entry + spaceSeparator
                                 + error.ErrorMessage;

            using (StreamWriter sw = File.AppendText(errorLogName))
            {
                sw.WriteLine(logTextLine);
            }
        }
    }
}
