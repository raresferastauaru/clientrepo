using System;
using System.IO;
using System.Linq;
using ClientApplication.APIs;
using ClientApplication.Models;

namespace ClientApplication.Processors
{
    public class SyncProcessor : IDisposable
    {
        private int _dequeuedFilesCounter = 1;
		public bool On { get; private set; }
		private CommandHandler _commandHandler;
		private ThreadSafeList<CustomFileHash> _changedFilesList;

		public SyncProcessor(CommandHandler commandHandler, ThreadSafeList<CustomFileHash> changedFilesList)
		{
			_commandHandler = commandHandler;
			_changedFilesList = changedFilesList;
		}

		public void Dispose()
		{
			_commandHandler = null;
			_changedFilesList = null;
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

        public async void ChangedFileManager()
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
		                            if (Helper.IsDirectory(fileHash.FullLocalPath))
			                            await _commandHandler.Mkdir(fileHash.RelativePath);
		                            else
			                            await _commandHandler.Put(fileHash);
                                    Logger.WriteLine(String.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                    break;

                                case FileChangeTypes.ChangedOnClient:
									await _commandHandler.Put(fileHash);
                                    Logger.WriteLine(String.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                    break;

                                case FileChangeTypes.DeletedOnClient:
									await _commandHandler.Delete(fileHash.RelativePath);
                                    Logger.WriteLine(String.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                    break;

                                case FileChangeTypes.RenamedOnClient:
									await _commandHandler.Rename(fileHash.OldRelativePath, fileHash.RelativePath);
                                    Logger.WriteLine(String.Format("{0}. Succes on dequeue: {1} renamed to {2}",
                                        _dequeuedFilesCounter++, fileHash.OldRelativePath, fileHash.RelativePath));
                                    break;


                                // Changes on SERVER
                                case FileChangeTypes.ChangedOnServer:
									var fileCreated = _commandHandler.Get(fileHash.RelativePath);
                                    if (await fileCreated)
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
		                            {
			                            File.Delete(fileHash.FullLocalPath);
			                            Logger.WriteLine(String.Format("{0}. Succes on dequeue: {1} ==> {2}",
				                            _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
		                            }
		                            else
		                            {
										Logger.WriteLine(String.Format("{0}. Fail on dequeue: {1} ==> {2}\n(File doesn't exist..)",
											_dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
		                            }
                                    break;

                                case FileChangeTypes.RenamedOnServer:
		                            File.Move(fileHash.OldFullLocalPath, fileHash.FullLocalPath);
									Logger.WriteLine(String.Format("{0}. Succes on dequeue ?: {1} ==> {2}",
										_dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
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
        }
    }
}
