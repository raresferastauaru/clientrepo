using ClientApplicationWpf.Model;
using System;
using System.Configuration;
using System.IO;
using System.Threading;

namespace ClientApplicationWpf
{
    public class Helper
    {
        static Helper()
        {
            HostIp = ConfigurationManager.AppSettings["HostIp"];
            HostPort = int.Parse(ConfigurationManager.AppSettings["HostPort"]);

            ValidateLoggingLocation();
            ValidateSyncLocation();

            BufferSize = int.Parse(ConfigurationManager.AppSettings["BufferSize"]);
            TraceEnabled = bool.Parse(ConfigurationManager.AppSettings["TraceEnabled"]);
        }

        #region ConfigKeys
        public static string HostIp { get; private set; }
        public static int HostPort { get; private set; }

        private static string _syncLocation;
        public static string SyncLocation
        {
            get { return _syncLocation; }
            set
            {
                if (_syncLocation != value)
                    _syncLocation = value;

                if (ConfigurationManager.AppSettings["SyncLocation"] != value)
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                    config.AppSettings.Settings["SyncLocation"].Value = value;
                    config.Save(ConfigurationSaveMode.Full, true);
                    ConfigurationManager.RefreshSection("appSettings");
                }
            }
        }


        private static string _loggerLocation;
        public static string LoggerLocation
        {
            get { return _loggerLocation; }
            set
            {
                if (_loggerLocation != value)
                    _loggerLocation = value;

                if (ConfigurationManager.AppSettings["LoggingLocation"] != value)
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                    config.AppSettings.Settings["LoggingLocation"].Value = value;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");

                    ValidateLoggingLocation();
                }
            }
        }


        private static string _currentLoggingFileLocation;
        public static string CurrentLoggingFileLocation
        {
            get
            {
                return _currentLoggingFileLocation;
            }
        }

        public static int BufferSize { get; private set; }
        public static bool TraceEnabled { get; private set; }

        #endregion ConfigKeys

        public static string GetRelativePath(string path)
        {
            var relativePath = path.Remove(0, SyncLocation.Length).Replace('\\', '/');
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
            _loggerLocation = ConfigurationManager.AppSettings["LoggingLocation"];
            _currentLoggingFileLocation = _loggerLocation + "Log_" + DateTime.Now.ToShortDateString().Replace("/", "_") + " (0).txt";

            var dirPath = Path.GetDirectoryName(CurrentLoggingFileLocation);
            if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string fileName = Path.GetFileNameWithoutExtension(CurrentLoggingFileLocation);
            string fileExt = Path.GetExtension(CurrentLoggingFileLocation);

            for (int i = 1; ; ++i)
            {
                if (File.Exists(CurrentLoggingFileLocation))
                    _currentLoggingFileLocation = _currentLoggingFileLocation.Replace("(" + (i - 1) + ")", "(" + i + ")");
                else
                    return;
            }
        }

        private static void ValidateSyncLocation()
        {
            _syncLocation = ConfigurationManager.AppSettings["SyncLocation"];

            if (!Directory.Exists(SyncLocation))
                Directory.CreateDirectory(SyncLocation);
        }
        #endregion PrivateMethods
    }
}