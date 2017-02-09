using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;

using ClientApplication.Models;
using ClientApplication.Processors;

namespace ClientApplication.APIs
{
    public class MyFsWatcher : IDisposable
    {
        private readonly FileSystemWatcher _fileWatcher;
        private readonly TcpCommunication _myTcp;
        private int _enqueuedFilesCounter = 1;
        private readonly SyncProcessor _syncProcessor;
        private readonly ConcurrentQueue<CustomFileHash> _temporaryQueue;

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public MyFsWatcher(String watcherPath, TcpCommunication myTcp)
        {
            _myTcp = myTcp;
            _syncProcessor = new SyncProcessor(_myTcp);
            _fileWatcher = new FileSystemWatcher
            {
                Path = watcherPath,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            //Maybe you shouldn't be waiting for it.
            //Maybe you should start recording the new changes on client 
            //  and start the SyncProcessor after the InitialSync is done
//            var initialSyncTask = 

			Task.Factory.StartNew(InitialSync());

//            Task.WaitAll(initialSyncTask);
//            initialSyncTask.Dispose();

            Logger.WriteSyncBreakLine();

            _fileWatcher.Created += OnChanged;
            _fileWatcher.Changed += OnChanged;
            _fileWatcher.Renamed += OnChanged;
            _fileWatcher.Deleted += OnChanged;

            Task.Factory.StartNew(_syncProcessor.ChangedFileManager());
        }
        public void Dispose()
        {
            _myTcp.Dispose();
            _fileWatcher.Dispose();
        }
        private Action InitialSync()
        {
            return () =>
            {
                Logger.WriteInitialSyncBreakLine();

                var serverFilesHashes = _myTcp.GetAllFileHashes();

                var paths = Directory.GetFiles(Helper.SyncLocation, "*", SearchOption.AllDirectories)
                                     .Where(p => !p.Equals(Helper.SyncLocation + "\\desktop.ini"))
                                     .ToList();
                var clientFileHashes = new List<CustomFileHash>();
                paths.ForEach(path => clientFileHashes.Add(new CustomFileHash(path)));

                var processedFileHashes = InitialSyncProcessorHelper.GetProcessedFileHashes(clientFileHashes, serverFilesHashes);

                foreach(var syncFileHash in processedFileHashes)
                    _syncProcessor.EnqueueChange(syncFileHash);
            };
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    if(Helper.IsDirectory(e.FullPath))
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

        // Maybe you sould register all the client changes on a temporary queue
        //      after initial sync you can put all changes on the normal queue
        // if you put a flag(here) "InitialSyncDone" and by that you should know in what queue to register all events
        // At the end of method InitialSync() you should move all the changes from temporaryQueue in normalQueue
        private void EnqueuingManager(FileChangeTypes fileChangeTypes, String fullPath, String oldFullPath = "")
        {
            if (_syncProcessor.Processing(fullPath)) return;
            
            var fileHash = new CustomFileHash(fileChangeTypes, fullPath, oldFullPath);
            _syncProcessor.EnqueueChange(fileHash);
            Logger.WriteFileHash(_enqueuedFilesCounter++, fileHash);
        }
        // Or maybe you could put a flag on FileHash: OnHold
        //      and on SyncProcessor you just have to check if the file is on hold or not
    }
}