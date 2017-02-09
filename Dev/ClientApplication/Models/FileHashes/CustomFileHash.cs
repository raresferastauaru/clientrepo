using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using ClientApplication.Models;

namespace ClientApplication.Models.FileHashes
{
    public class CustomFileHash : AbstractFileHash
    {
        #region PrivateMembers
        private readonly FileChangeTypes _changeType;
        private readonly string _name;
        private readonly string _relativePath;
        private readonly string _fullPath;
        private readonly string _oldRelativePath;
        private readonly bool _wasReadOnly;
        private readonly long _size;
        private readonly DateTime _created;
        private readonly DateTime _modified;
        private readonly DateTime _accessed;
        private readonly int _hashCode;
        #endregion PrivateMembers

        #region Constructors
        public CustomFileHash(FileChangeTypes changeType, string fullPath, string oldFullPath = "")
        {
            _changeType = changeType;

            _name = Path.GetFileName(fullPath);
            _relativePath = Helper.GetRelativePath(fullPath);
            _fullPath = fullPath;

            if (!Helper.IsDirectory(_fullPath))
                _hashCode = base.GetHashCode();

            if (changeType == FileChangeTypes.RenamedOnClient)
                _oldRelativePath = Helper.GetRelativePath(oldFullPath);

            if ((changeType == FileChangeTypes.ChangedOnClient || changeType == FileChangeTypes.RenamedOnClient)
                && !Helper.IsDirectory(fullPath))
            {
                var info = new FileInfo(_fullPath);
                _wasReadOnly = info.IsReadOnly;
                _size = info.Length;
                _created = info.CreationTime;
                _modified = info.LastWriteTime;
                _accessed = info.LastAccessTime;
            }
        }
        #endregion Constructors

        #region Overrides
        public override FileChangeTypes ChangeType
        {
            get { return _changeType; }    
        }

        public override string Name
        {
            get { return _name; }
        }

        // Maybe you should rename it to "LocalPath" or "FullLocalPath"
        public override string FullPath
        {
            get { return _fullPath; }
        }

        public override string RelativePath
        {
            get { return _relativePath; }
        }

        public override string OldRelativePath
        {
            get { throw new NotImplementedException(); }
        }

        public override int HashCode
        {
            get { return _hashCode; }
        }
        #endregion Overrides

        #region PublicMembers
        public int OldHashCode
        {
            get { throw new NotImplementedException(); }
        }

        public bool WasReadOnly
        {
            get { return _wasReadOnly; }
        }

        public long Size
        {
            get { return _size; }
        }

        public DateTime Created
        {
            get { return _created; }
        }

        public DateTime Modified
        {
            get { return _modified; }
        }

        public DateTime Accessed
        {
            get { return _accessed; }
        }
        #endregion PublicMembers
    }
}

/*
    !!!WatcherChangeTypes:
                * Delete => crapa la info.Length(dar nici e nevoie-> trebuie doar numele)
                * Renamed si Changed trebuie sa pastreze istoricul
            
    !!! Va fi o diferenta intre coada de fisiere modificate, si lista de fisiere care trebuie pastrata in 
        memorie pentru sincronizare.
            * Oarecum: la pornirea aplicatiei ar trebuii inscrisa lista cu toate 
                       fisierele din folderul de sincronizare si comparate cu o lista
                       care va veni de la serviciu => Unde apar modificari, fisierul 
                       va fi adaugat in coada pentru PUT/GET
*/