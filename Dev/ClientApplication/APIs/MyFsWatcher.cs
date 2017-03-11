﻿using System;
using System.IO;
using System.Security.Permissions;

using ClientApplication.Models;
using ClientApplication.Processors;

namespace ClientApplication.APIs
{
    public class MyFsWatcher : IDisposable
	{
		private int _enqueuedFilesCounter = 1;
		private SyncProcessor _syncProcessor;
        private FileSystemWatcher _fileWatcher;

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public MyFsWatcher(String watcherPath, SyncProcessor syncProcessor)
        {
			_syncProcessor = syncProcessor;
            _fileWatcher = new FileSystemWatcher
            {
                Path = watcherPath,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _fileWatcher.Created += OnChanged;
            _fileWatcher.Changed += OnChanged;
            _fileWatcher.Renamed += OnChanged;
			_fileWatcher.Deleted += OnChanged;
        }
        public void Dispose()
        {
            _fileWatcher.Dispose();
	        _fileWatcher = null;
	        _syncProcessor = null;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    EnqueuingManager(FileChangeTypes.CreatedOnClient, e.FullPath);
                    break;
                case WatcherChangeTypes.Changed:
                    if(!Helper.IsDirectory(e.FullPath))
                        EnqueuingManager(FileChangeTypes.ChangedOnClient, e.FullPath);
                    break;
                case WatcherChangeTypes.Deleted:
		            if (Helper.IsDirectory(e.FullPath))
			            EnqueuingManager(FileChangeTypes.DeletedOnClient, e.FullPath);
		            else
		            {
			            var path = Path.GetDirectoryName(e.FullPath);
						if (path != null && Directory.Exists(path))
							EnqueuingManager(FileChangeTypes.DeletedOnClient, e.FullPath);
		            }
                    break;
                case WatcherChangeTypes.Renamed:
                    var rea = e as RenamedEventArgs;
                    if(rea != null)
                        EnqueuingManager(FileChangeTypes.RenamedOnClient, rea.FullPath, rea.OldFullPath);
                    break;
            }
        }

        private void EnqueuingManager(FileChangeTypes fileChangeType, String fullPath, String oldFullPath = "")
		{
			var relativePath = Helper.GetRelativePath(fullPath);
			if (_syncProcessor.InProcessingList(relativePath))
			{
				return;
			}

			var fileHash = new CustomFileHash(fileChangeType, fullPath, oldFullPath);
			_syncProcessor.AddChangedFile(fileHash);

			Logger.WriteFileHash(_enqueuedFilesCounter++, fileHash);
        }
    }
}