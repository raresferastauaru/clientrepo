using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ClientApplication.Models;

namespace ClientApplication
{
    public static class Logger
    {
        private static int _enqueuedFilesCounter = 1;
        private static bool _traceEnabled;

        public static void InitLogger(bool traceEnabled = false)
		{
			if (traceEnabled)
			{
				_traceEnabled = true;
				Helper.TraceItems = new List<string>();
			}
        }

        public static void WriteLine(string message)
        {
            try
            {
	            var msg = String.Format("{0} : {1}\n", DateTime.Now, message);
				File.AppendAllText(Helper.LogingLocation, msg);

				if (_traceEnabled)
					Helper.TraceItems.Insert(0, msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Exception on logger: " + ex.Message);
            }
        }

        public static void WriteFileHash(CustomFileHash customFileHash)
        {
            var str = string.Format(
                "{0}. File Change Enqueued:\n\tRelativePath: {1}\n\tChangeType: {2}\n\tHashCode: {3}\n\tReadOnly: {4}\n",
                _enqueuedFilesCounter++,
                customFileHash.RelativePath,
                customFileHash.ChangeType,
                customFileHash.HashCode,
                customFileHash.WasReadOnly);
            
            WriteLine(str);
        }

        public static void WriteInitialSyncBreakLine()
        {
            try
            {
                File.AppendAllText(Helper.LogingLocation, "============================== Initial Sync Area ===================================\n\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Exception on logger: " + ex.Message);
            }
        }

        public static void WriteSyncBreakLine()
        {
            try
            {
                File.AppendAllText(Helper.LogingLocation, "\n================================== Sync Area =======================================\n\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Exception on logger: " + ex.Message);
            }
        }

        public static void WriteDisconnectLine()
        {
            try
            {
                File.AppendAllText(Helper.LogingLocation, "\n\n================================= Disconnected =====================================\n\n\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Exception on logger: " + ex.Message);
            }
        }

        public static void OpenTheLogFile()
        {
            Process.Start("notepad++.exe", Helper.LogingLocation);
        }
    }
}
