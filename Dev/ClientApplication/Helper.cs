using ClientApplication.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading;

namespace ClientApplication
{
    public class Helper
    {
	    static Helper()
	    {
            LogingLocation = ConfigurationManager.AppSettings["LogingLocation"] + "Log_" +
                                   DateTime.Now.ToShortDateString().Replace("/", "_") + " (0).txt";
            ValidateLoggingLocation();
            SyncLocation = ConfigurationManager.AppSettings["SyncLocation"];
            ValidateSyncLocation();
            BufferSize = int.Parse(ConfigurationManager.AppSettings["BufferSize"]);
		    TraceEnabled = bool.Parse(ConfigurationManager.AppSettings["TraceEnabled"]);

			TraceItems = new List<string>();
	    }

        public static string LogingLocation { get; private set; }
		public static string SyncLocation { get; private set; }
		public static int BufferSize { get; private set; }
		public static bool TraceEnabled { get; private set; }
		public static List<string> TraceItems { get; set; } 
		
        public static string GetRelativePath(string path)
        {
			var relativePath = path.Remove(0, SyncLocation.Length).Replace('\\','/');
	        return relativePath;
        }
        public static string GetLocalPath(string relativePath)
        {
            return string.Format("{0}{1}", SyncLocation, relativePath.Replace('/', '\\'));
        }
		
	    public static bool IsDirectory(string path)
	    {
		    try
		    {
			    return ((File.GetAttributes(path) & FileAttributes.Directory) != 0);
		    }
		    catch (FileNotFoundException)
		    {
			    return false;
		    } 
            catch (DirectoryNotFoundException)
            {
                var directoruPath = Path.GetDirectoryName(path);
                Directory.CreateDirectory(directoruPath);
                return false;
            }
	    }

	    public static bool IsFileLocked(string fullPath)
        {
            var fileInfo = new FileInfo(fullPath);
            FileStream stream = null;
            
            try
            {
                stream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                    return true;
                }
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        public static void ValidateDirectoryForFile(string fileName)
        {
            var dirPath = Path.GetDirectoryName(fileName);
            var fullPath = SyncLocation + dirPath;

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }
		public static bool ChangeFileAttributes(CustomFileHash customFileHash, string creationTimeTicks, string lastWriteTimeTicks, string isReadOnlyString)
        {
            var fullPath = GetLocalPath(customFileHash.RelativePath);

            var numberOfRetries = 5;
            var delayOnRetry = 100;
            for (int i = 1; i <= numberOfRetries; ++i)
            {
                try
                {
                    if (customFileHash.FileInfo == null)
                        customFileHash.FileInfo = new FileInfo(fullPath);

                    customFileHash.FileInfo.CreationTimeUtc = new DateTime(long.Parse(creationTimeTicks));
                    customFileHash.FileInfo.LastWriteTimeUtc = new DateTime(long.Parse(lastWriteTimeTicks));

                    var isReadOnly = bool.Parse(isReadOnlyString);
                    var fileAttributes = File.GetAttributes(fullPath);
                    if (isReadOnly)
                        fileAttributes |= FileAttributes.ReadOnly;

                    customFileHash.FileInfo.Attributes = fileAttributes;

                    return true;
                }
                catch (IOException ex)
                {
                    var msg = string.Format("Helper (ChangeFileAttributes - {0}): \n\tRetry number: {1} of {2}\n\tMessage: {3}\n\tStackTrace: {4}", 
                        fullPath, i, numberOfRetries, ex.Message, ex.StackTrace);
                    Logger.WriteLine(msg);

                    if (i == numberOfRetries)
                        return false;

                    Thread.Sleep(delayOnRetry);
                }
            }

            Logger.WriteLine("Helper - ChangeFileAttributes - WHY DID IT GOT HERE ?!");
            return false;
        }



        #region PrivateMethods
        private static void ValidateLoggingLocation()
        {
            var dirPath = Path.GetDirectoryName(LogingLocation);
            if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string fileName = Path.GetFileNameWithoutExtension(LogingLocation);
            string fileExt = Path.GetExtension(LogingLocation);

            for (int i = 1; ; ++i)
            {
                if (File.Exists(LogingLocation))
                    LogingLocation = LogingLocation.Replace("(" + (i - 1) + ")", "(" + i + ")");
                //Path.Combine(dirPath, fileName + " (" + i + ")" + fileExt);
                else
                    return;
            }

            //if (File.Exists(LogingLocation))
            //    File.Delete(LogingLocation);
        }
        private static void ValidateSyncLocation()
        {
            if (!Directory.Exists(SyncLocation))
                Directory.CreateDirectory(SyncLocation);
        }
        #endregion PrivateMethods
    }
}
