using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ClientApplication.Models;

namespace ClientApplication
{
    public static class Logger
    {
		private static bool _traceEnabled;
        public static void InitLogger(bool traceEnabled = false)
		{
			var dirPath = Path.GetDirectoryName(Helper.LogingLocation);
			if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
				Directory.CreateDirectory(dirPath);

            if(File.Exists(Helper.LogingLocation))
                File.Delete(Helper.LogingLocation);

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

        public static void WriteFileHash(int counter, CustomFileHash customFileHash)
        {
            var str = string.Format(
                "{0}. File Change Enqueued:\n\tRelativePath: {1}\n\tChangeType: {2}\n\tHashCode: {3}\n\tReadOnly: {4}\n",
                counter,
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
                File.AppendAllText(Helper.LogingLocation,
                     @"============================== Initial Sync Area ===================================\n\n");
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
                File.AppendAllText(Helper.LogingLocation,
                     @"\n============================== Sync Area ===================================\n\n");
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
