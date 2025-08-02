using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQM.Console
{
    public class ErrorLog
    {
        public static void LogErrorMessage(string errorMessage, string logFolder)
        {
            string fileName = "Error_logFile_" + DateTime.Now.Date.Ticks + ".txt";
            if (String.IsNullOrEmpty(logFolder))
            {
                logFolder = Path.Combine("ErrorLogs");
                if (!System.IO.Directory.Exists(logFolder))
                    System.IO.Directory.CreateDirectory(logFolder);
            }

            //string logFile = Path.Combine("ErrorLogs", fileName);
            string logFile = Path.Combine(logFolder, fileName);
            if (!System.IO.File.Exists(logFile))
            {
                System.IO.File.Create(logFile).Close();
            }

            System.IO.File.AppendAllText(logFile, DateTime.Now + " " + errorMessage + Environment.NewLine);
        }
    }
}
