using ClientApplicationWpf.Messages;
using ClientApplicationWpf.Model;
using ClientApplicationWpf.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ClientApplicationWpf
{
    public static class Logger
    {
        private static int _enqueuedFilesCounter = 1;
        private static bool _traceEnabled;
        //public static ObservableCollection<TraceItem> TraceItems { get; private set; }

        public static void InitLogger(bool traceEnabled)
        {
            if (traceEnabled)
            {
                //TraceItems = new ObservableCollection<TraceItem>();
                _traceEnabled = true;
            }
        }

        public static void WriteLine(string message)
        {
            try
            {
                var msg = String.Format("{0} : {1}\n", DateTime.Now, message);
                File.AppendAllText(Helper.CurrentLoggingFileLocation, msg);

                if (_traceEnabled)
                {
                    //TraceItems.Add(new TraceItem(msg));
                    Messenger.Default.Send(new TraceItemAddedMsg(msg));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Exception on logger: " + ex.Message);
            }
        }

        public static void WriteFileHash(CustomFileHash customFileHash)
        {
            var str = string.Format(
                "{0}. File Change Enqueued: RelativePath: {1} - ChangeType: {2}\n\tHashCode: {3}\n\tReadOnly: {4}\n",
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
                File.AppendAllText(Helper.CurrentLoggingFileLocation, "============================== Initial Sync Area ===================================\n\n");
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
                File.AppendAllText(Helper.CurrentLoggingFileLocation, "\n================================== Sync Area =======================================\n\n");
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
                File.AppendAllText(Helper.CurrentLoggingFileLocation, "\n\n================================= Disconnected =====================================\n\n\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Exception on logger: " + ex.Message);
            }
        }

        public static void OpenTheLogFile()
        {
            Process.Start("notepad++.exe", Helper.CurrentLoggingFileLocation);
        }
    }
}
