using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;

using ClientApplication.APIs;
using ClientApplication.Models;

namespace ClientApplication.Processors
{
    public class SyncProcessor
    {
        private int _dequeuedFilesCounter = 1;
        private readonly TcpCommunication _myTcp;
        private readonly ConcurrentQueue<CustomFileHash> _changedFilesQueue;

		public bool SyncProcessorEnabled { get; set; }

        public SyncProcessor(TcpCommunication myTcp)
        {
            _myTcp = myTcp;
            _changedFilesQueue = new ConcurrentQueue<CustomFileHash>();
        }

        public Action ChangedFileManager()
        {
            return () =>
            {
                while (true)
                {
                    try
                    {
                        if (SyncProcessorEnabled && !_changedFilesQueue.IsEmpty)
                        {
                            var processed = false;
                            var fileHash = _changedFilesQueue.First();
                            if (!File.Exists(fileHash.FullLocalPath) || (File.Exists(fileHash.FullLocalPath) && !Helper.IsFileLocked(fileHash.FullLocalPath)))
                            {
                                switch (fileHash.ChangeType)
                                {
                                    // Changes on CLIENT
                                    case FileChangeTypes.CreatedOnClient:
                                        _myTcp.Mkdir(fileHash.RelativePath);
                                        Logger.WriteLine(String.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                            _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                        break;

                                    case FileChangeTypes.ChangedOnClient:
                                        _myTcp.Put(fileHash);
                                        Logger.WriteLine(String.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                            _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                        break;

                                    case FileChangeTypes.DeletedOnClient:
                                        _myTcp.Delete(fileHash.RelativePath);
                                        Logger.WriteLine(String.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                            _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                        break;

                                    case FileChangeTypes.RenamedOnClient:
                                        _myTcp.Rename(fileHash.OldRelativePath, fileHash.RelativePath);
                                        Logger.WriteLine(String.Format("{0}. Succes on dequeue: {1} renamed to {2}",
                                            _dequeuedFilesCounter++, fileHash.OldRelativePath, fileHash.RelativePath));
                                        break;


                                    // Changes on SERVER
                                    case FileChangeTypes.ChangedOnServer:
                                        var fileCreated = _myTcp.Get(fileHash.RelativePath);
                                        if (fileCreated)
                                        {
                                            Logger.WriteLine(String.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                                _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                        }
                                        else
                                        {
                                            Logger.WriteLine(String.Format("{0}. Fail on dequeue: {1} ==> {2}\n(File was empty on server, or something else..)",
                                                _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                        }
                                        break;

                                    case FileChangeTypes.DeletedOnServer:
                                        if (File.Exists(fileHash.FullLocalPath)) 
                                            File.Delete(fileHash.FullLocalPath);
                                        break;

                                    case FileChangeTypes.RenamedOnServer:
		                                File.Move(Helper.GetLocalPath(fileHash.OldRelativePath), fileHash.FullLocalPath);
		                                break;
                                }

                                processed = true;
                            }

                            if (_changedFilesQueue.TryDequeue(out fileHash))
                            {
                                if (!processed)
                                    _changedFilesQueue.Enqueue(fileHash);
                            }
                            else
                            {
                                Logger.WriteLine("Queue is empty. HOW ?!");
                            }
                        }
                        else
                        {
                            // The queue is EMPTY or the processor is blocked.. wait 1 sec and then check for changes
                            Thread.Sleep(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine("\t!!!\tEXCEPTION: on MyFSWatcher: " + ex.Message + "\t!!!");
                    }
                }
            };
        }

        public void EnqueueChange(CustomFileHash customFileHash)
        {
            _changedFilesQueue.Enqueue(customFileHash);
        }

        public bool Processing(string fullPath)
        {
            return _changedFilesQueue.Any(f => String.Equals(f.FullLocalPath, fullPath));
        }
    }
}
