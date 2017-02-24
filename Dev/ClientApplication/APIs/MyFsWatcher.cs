using System;
using System.IO;
using System.Security.Permissions;
using System.Threading.Tasks;

using ClientApplication.Models;
using ClientApplication.Processors;

namespace ClientApplication.APIs
{
    public class MyFsWatcher : IDisposable
	{
		private int _enqueuedFilesCounter = 1;
		private readonly SyncProcessor _syncProcessor;
        private readonly FileSystemWatcher _fileWatcher;

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
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
					//if(Helper.IsDirectory(e.FullPath))
                    EnqueuingManager(FileChangeTypes.CreatedOnClient, e.FullPath);
                    break;
                case WatcherChangeTypes.Changed:
                    if(!Helper.IsDirectory(e.FullPath))
                        EnqueuingManager(FileChangeTypes.ChangedOnClient, e.FullPath);
                    break;
                case WatcherChangeTypes.Deleted:
                    EnqueuingManager(FileChangeTypes.DeletedOnClient, e.FullPath);
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
			if (_syncProcessor.InProcessingList(relativePath)) // && fileChangeType == FileChangeTypes.ChangedOnClient ???
			{
				return;
			}

			var fileHash = new CustomFileHash(fileChangeType, fullPath, oldFullPath);
			_syncProcessor.AddChangedFile(fileHash);

	        if (!_syncProcessor.On)
	        {
				var task = new Task(_syncProcessor.ChangedFileManager);
				task.Start();
	        }

			Logger.WriteFileHash(_enqueuedFilesCounter++, fileHash);
        }
    }
}




/*
private void EnqueuingManager(FileChangeTypes fileChangeTypes, String fullPath, String oldFullPath = "")
{
//if(_syncProcessor.)
//Task.Factory.StartNew(_syncProcessor.ChangedFileManager());
	  
	var relativePath = Helper.GetRelativePath(fullPath);
	if (_syncProcessor.InProcessingList(relativePath))
	{
		if (fileChangeTypes == FileChangeTypes.RenamedOnClient)
		{
					
		}
		// File will always exists (you can't make a change on a file that doesn't exists :D)
		else if (fileChangeTypes == FileChangeTypes.DeletedOnClient || !Helper.IsFileLocked(fullPath))
		{
			_syncProcessor.RemoveFileHash(relativePath);
		}
		else
		{
			return;
		}
	}

	var fileHash = new CustomFileHash(fileChangeTypes, fullPath, oldFullPath);
	_syncProcessor.AddChangedFile(fileHash);
	Logger.WriteFileHash(_enqueuedFilesCounter++, fileHash);
}
*/