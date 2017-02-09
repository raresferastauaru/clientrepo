using System;
using System.Diagnostics;
using System.IO;
using ClientApplication.Models;

namespace ClientApplication
{
    public static class Logger
    {
        public static void InitLogger()
        {
            if(File.Exists(Helper.LogingLocation))
                File.Delete(Helper.LogingLocation);
        }

        public static void WriteLine(string message)
        {
            try
            {
                File.AppendAllText(Helper.LogingLocation, String.Format("{0} : {1}\n", DateTime.Now , message));
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Exception on logger: " + ex.Message);
            }
        }

        public static void WriteFileHash(int counter, CustomFileHash customFileHash)
        {
            var str = string.Format(
                "{0}. File Change Enqueued:\n\tRelativePath: {1}\n\tChangeType: {2}\n\tHashCode: {3}\n\tReadOnly: {4}",
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
