using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace ClientApplicationWpf.Model
{
    public class CustomFileHash : IDisposable
    {
        #region Properties
        public FileChangeTypes ChangeType { get; private set; }
        public string FullLocalPath { get; private set; }
        public string RelativePath { get; private set; }
        public string OldFullLocalPath { get; private set; }
        public string OldRelativePath { get; private set; }
        public int HashCode { get; private set; }
        public int OldHashCode { get; private set; }
        public bool WasReadOnly { get; private set; }
        public bool IsDeleted { get; private set; }
        public FileInfo FileInfo { get; set; }
        public FileStream FileStream { get; set; }
        #endregion Properties

        #region Constructors
        /// <summary>
        /// Opened on Main for Client Files Hash
        /// </summary>
        /// <param name="fullLocalPath"></param>
        public CustomFileHash(string fullLocalPath)
        {
            ChangeType = FileChangeTypes.None;
            FullLocalPath = fullLocalPath;
            RelativePath = Helper.GetRelativePath(fullLocalPath);
            OldRelativePath = RelativePath;
            OldHashCode = 0;

            if (File.Exists(fullLocalPath))
            {
                InitFileStream();
                HashCode = GetFilesHashCode();
                FileStream.Close();

                FileInfo = new FileInfo(fullLocalPath);
            }
        }

        /// <summary>
        /// Used on EnqueuingManager from MyFsWatcher + on TcpCommunication (PushNotification)
        /// </summary>
        /// <param name="changeType"></param>
        /// <param name="fullLocalPath"></param>
		/// <param name="oldFullLocalPath"></param>
        public CustomFileHash(FileChangeTypes changeType, string fullLocalPath, string oldFullLocalPath = "")
        {
            ChangeType = changeType;
            FullLocalPath = fullLocalPath;
            RelativePath = Helper.GetRelativePath(fullLocalPath);

            if (changeType == FileChangeTypes.DeletedOnClient
                || changeType == FileChangeTypes.CreatedOnServer)
                return;
            else if (changeType == FileChangeTypes.RenamedOnServer)
            {
                OldFullLocalPath = oldFullLocalPath;
                return;
            }
            else if (changeType == FileChangeTypes.RenamedOnClient)
                OldRelativePath = Helper.GetRelativePath(oldFullLocalPath);

            if (!Helper.IsDirectory(fullLocalPath))
            {
                InitFileStream();
                HashCode = GetFilesHashCode();
                FileStream.Position = 0;

                FileInfo = new FileInfo(fullLocalPath);
                if (changeType == FileChangeTypes.ChangedOnClient || changeType == FileChangeTypes.RenamedOnClient)
                {
                    WasReadOnly = FileInfo.IsReadOnly;
                }
            }
        }

        private void InitFileStream()
        {
            if (File.Exists(FullLocalPath))
                while (Helper.IsFileLocked(FullLocalPath))
                    Thread.Sleep(500);

            // The stream must be opened after the hash is received !
            FileStream = File.Open(FullLocalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        /// <summary>
        /// Used in InitialSyncProcessor and on GetAllFileHashes
        /// </summary>
        /// <param name="changeType"></param>
        /// <param name="relativePath"></param>
        /// <param name="oldRelativePath"></param>
        /// <param name="hashCode"></param>
        /// <param name="oldHashCode"></param>
        /// <param name="isDeleted"></param>
        public CustomFileHash(FileChangeTypes changeType, string relativePath, string oldRelativePath,
            int hashCode, int oldHashCode, bool isDeleted = false)
        {
            ChangeType = changeType;
            RelativePath = relativePath;
            FullLocalPath = Helper.GetLocalPath(relativePath);
            OldRelativePath = oldRelativePath;
            OldFullLocalPath = Helper.GetLocalPath(oldRelativePath);
            HashCode = hashCode;
            OldHashCode = oldHashCode;
            IsDeleted = isDeleted;

            if (changeType == FileChangeTypes.ChangedOnClient || changeType == FileChangeTypes.ChangedOnServer || changeType == FileChangeTypes.DeletedOnServer)
                FullLocalPath = Helper.GetLocalPath(RelativePath);

            if (File.Exists(FullLocalPath))
            {
                FileInfo = new FileInfo(FullLocalPath);
            }

            if (changeType != FileChangeTypes.None
                && changeType != FileChangeTypes.RenamedOnClient
                && changeType != FileChangeTypes.RenamedOnServer)
            {
                Helper.ValidateDirectoryForFile(RelativePath);
                FileStream = File.Open(FullLocalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
        }

        public void Dispose()
        {
            if (FileStream != null)
                FileStream.Dispose();

            FileInfo = null;
        }
        #endregion Constructors

        #region Methods
        public int GetFilesHashCode()
        {
            using (var md5 = MD5.Create())
            {
                var info = new FileInfo(FullLocalPath);

                var infoStr = info.CreationTimeUtc.ToString(CultureInfo.InvariantCulture)
                                + info.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)
                                + info.IsReadOnly;

                var infoHash = infoStr.GetHashCode();

                var md5Hash = BitConverter.ToInt32(md5.ComputeHash(FileStream), 0);

                md5Hash += infoHash;
                return md5Hash.GetHashCode();
            }
        }

        public string GetFileHashDetails()
        {
            var str = "FileHashDetails:"
                      + HashCode + ":"
                      + FileInfo.CreationTimeUtc.Ticks + ":"
                      + FileInfo.LastWriteTimeUtc.Ticks + ":"
                      + FileInfo.IsReadOnly + ":";
            return str;
        }

        public string GetFileHashBasicDetails()
        {
            var str = HashCode + ":";
            if (FileInfo != null)
            {
                str += FileInfo.CreationTime + ":"
                    + FileInfo.LastWriteTime + ":"
                    + FileInfo.IsReadOnly + ":";
            }
            return str;
        }

        public override string ToString()
        {
            var str = string.Empty;

            if (!string.IsNullOrEmpty(FullLocalPath))
                str += "FullLocalPath: " + FullLocalPath + "\n";

            if (!string.IsNullOrEmpty(RelativePath))
                str += "RelativePath: " + RelativePath + "\n";

            if (!string.IsNullOrEmpty(OldRelativePath))
                str += "OldRelativePath: " + OldRelativePath + "\n";

            str += "HashCode: " + HashCode + "\n";
            str += "OldHashCode: " + OldHashCode + "\n";
            str += "WasReadOnly: " + WasReadOnly + "\n";

            if (FileInfo != null)
                str += "Size: " + FileInfo.Length + "\n";

            str += "FileInfo: " + FileInfo + "\n";
            str += "ChangeType: " + ChangeType + "\n";

            return str;
        }
        #endregion Methods
    }
}
