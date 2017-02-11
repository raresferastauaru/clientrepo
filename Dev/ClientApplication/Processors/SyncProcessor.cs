using System;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;

using ClientApplication.APIs;
using ClientApplication.Models;

namespace ClientApplication.Processors
{
    public class SyncProcessor
    {
        private int _dequeuedFilesCounter = 1;
		public bool On { get; private set; }
		private readonly TcpCommunication _myTcp;
		private readonly ThreadSafeList<CustomFileHash> _changedFilesList;

		public SyncProcessor(TcpCommunication myTcp)
		{
			_myTcp = myTcp;
			_changedFilesList = new ThreadSafeList<CustomFileHash>();
		}

		public void AddChangedFile(CustomFileHash customFileHash)
		{
			_changedFilesList.Add(customFileHash);
		}

		public bool InProcessingList(string relativePath)
		{
			return _changedFilesList.Any(f => f.RelativePath.Equals(relativePath));
		}

		public void RemoveFileHash(string relativePath)
		{
			var fileHash = _changedFilesList.First(f => f.RelativePath == relativePath);
			_changedFilesList.Remove(fileHash);
		}

        public Action ChangedFileManager()
        {
            return () =>
            {
				On = true;
                while (true)
                {
					CustomFileHash fileHash = null;
                    try
                    {
                        if (_changedFilesList.Count != 0)
                        {
	                        fileHash = _changedFilesList.First();
                            var processed = false;
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
							
							_changedFilesList.Remove(fileHash);
							if(!processed)
								_changedFilesList.Add(fileHash);
                        }
                        else
                        {
                            // The queue is EMPTY or the processor is blocked.. wait 1 sec and then check for changes
                            // Thread.Sleep(1000);
	                        On = false;
	                        return;
                        }
                    }
                    catch (Exception ex)
                    {
	                    if (fileHash != null)
	                    {
		                    _changedFilesList.Remove(fileHash);
	                    }

                        Logger.WriteLine("\t!!!\tEXCEPTION: on MyFSWatcher: " + ex.Message + "\t!!!");
                    }
                }
            };
        }
    }
}
