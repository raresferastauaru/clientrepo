using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace ClientApplication.Models
{
    public class CustomFileHash
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
        public long Size { get; private set; }
        public FileInfo FileInfo { get; private set; }
	    #endregion Properties

        #region Constructors
        /// <summary>
        /// Opened on InitialSync for Client Files Hash
        /// </summary>
        /// <param name="fullLocalPath"></param>
        public CustomFileHash(string fullLocalPath)
        {
            ChangeType = FileChangeTypes.None;
            FullLocalPath = fullLocalPath;
            RelativePath = Helper.GetRelativePath(fullLocalPath);
            OldRelativePath = RelativePath;
            HashCode = GetFilesHashCode();
            OldHashCode = 0;

            if (File.Exists(fullLocalPath))
                FileInfo = new FileInfo(fullLocalPath);

        }

        /// <summary>
        /// Used on EnqueuingManager from MyFsWatcher
        /// </summary>
        /// <param name="changeType"></param>
        /// <param name="fullLocalPath"></param>
		/// <param name="oldFullLocalPath"></param>
        public CustomFileHash(FileChangeTypes changeType, string fullLocalPath, string oldFullLocalPath = "")
        {
            ChangeType = changeType;
            FullLocalPath = fullLocalPath;
            RelativePath = Helper.GetRelativePath(fullLocalPath);
            FileInfo = new FileInfo(FullLocalPath);

            if (changeType == FileChangeTypes.RenamedOnClient)
                OldRelativePath = Helper.GetRelativePath(oldFullLocalPath);
            if (changeType == FileChangeTypes.RenamedOnServer)
                OldFullLocalPath = oldFullLocalPath;

            if (!File.Exists(FullLocalPath)) return;

            if (!Helper.IsDirectory(FullLocalPath))
                HashCode = GetFilesHashCode();

            if ((changeType == FileChangeTypes.ChangedOnClient || changeType == FileChangeTypes.RenamedOnClient)
                && !Helper.IsDirectory(fullLocalPath))
            {
                WasReadOnly = FileInfo.IsReadOnly;
                Size = FileInfo.Length;
            }

            if (File.Exists(fullLocalPath))
                FileInfo = new FileInfo(fullLocalPath);
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
            OldRelativePath = oldRelativePath;
            HashCode = hashCode;
            OldHashCode = oldHashCode;
	        IsDeleted = isDeleted;

            if (changeType == FileChangeTypes.ChangedOnClient || changeType == FileChangeTypes.ChangedOnServer || changeType == FileChangeTypes.DeletedOnServer)
                FullLocalPath = Helper.GetLocalPath(RelativePath);

            if (File.Exists(FullLocalPath))
                FileInfo = new FileInfo(FullLocalPath);
        }
        #endregion Constructors

        #region Methods
        public int GetFilesHashCode()
        {
            using (var md5 = MD5.Create())
            {
                while (Helper.IsFileLocked(FullLocalPath))
                {
                    Thread.Sleep(500);
                }

                using (var stream = File.OpenRead(FullLocalPath))
                {
                    var info = new FileInfo(FullLocalPath);

                    var infoStr = info.CreationTimeUtc.ToString(CultureInfo.InvariantCulture)
                                    + info.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)
                                    + info.IsReadOnly;
                    
                    var infoHash = infoStr.GetHashCode();

                    var md5Hash = BitConverter.ToInt32(md5.ComputeHash(stream), 0);

                    md5Hash += infoHash;
                    return md5Hash.GetHashCode();
                }
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
            str += "Size: " + Size + "\n";
            str += "FileInfo: " + FileInfo + "\n";
            str += "ChangeType: " + ChangeType + "\n";

            return str;
        }
        #endregion Methods
    }
}