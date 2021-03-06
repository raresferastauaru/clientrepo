﻿using System;
using System.IO;
using System.Linq;
using ClientApplication.APIs;
using ClientApplication.Models;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task ChangedFileManager()
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
                        switch (fileHash.ChangeType)
                        {
                            // Changes on CLIENT
                            case FileChangeTypes.CreatedOnClient:
                                if (Helper.IsDirectory(fileHash.FullLocalPath))
                                    await _commandHandler.Mkdir(fileHash);
                                else
                                    await _commandHandler.Put(fileHash);
                                Logger.WriteLine(string.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                    _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                break;

                            case FileChangeTypes.ChangedOnClient:
                                await _commandHandler.Put(fileHash);
                                Logger.WriteLine(string.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                    _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                break;

                            case FileChangeTypes.RenamedOnClient:
                                await _commandHandler.Rename(fileHash);
                                Logger.WriteLine(string.Format("{0}. Succes on dequeue: {1} renamed to {2}",
                                    _dequeuedFilesCounter++, fileHash.OldRelativePath, fileHash.RelativePath));
                                break;

                            case FileChangeTypes.DeletedOnClient:
                                await _commandHandler.Delete(fileHash);
                                Logger.WriteLine(string.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                    _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                break;


                            // Changes on SERVER
                            case FileChangeTypes.CreatedOnServer:
                                //if (Helper.IsDirectory(fileHash.FullLocalPath))
                                //{
                                    Directory.CreateDirectory(fileHash.FullLocalPath);
                                    Logger.WriteLine(string.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                //}
                                //else
                                //{
                                //    Logger.WriteLine(string.Format("{0}. Fail on dequeue: {1} ==> {2} - It should've been a folder here.",
                                //        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                //}
                                break;

                            case FileChangeTypes.ChangedOnServer:
                                var fileCreated = _commandHandler.Get(fileHash);
                                if (await fileCreated)
                                    Logger.WriteLine(string.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                else
                                    Logger.WriteLine(string.Format("{0}. Fail on dequeue: {1} ==> {2}",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                break;

                            case FileChangeTypes.RenamedOnServer:
                                if (Helper.IsDirectory(fileHash.OldFullLocalPath))
                                {
                                    Directory.Move(fileHash.OldFullLocalPath, fileHash.FullLocalPath);

                                    //var tempName = Helper.SyncLocation + "~RenamingFolder~";
                                    //var dir = new DirectoryInfo(fileHash.OldFullLocalPath);
                                    //dir.MoveTo(tempName);
                                    //Thread.Sleep(2000);
                                    //dir.MoveTo(fileHash.FullLocalPath);
                                }
                                else
                                {
                                    //fileHash.FileStream.Close();
                                    File.Move(fileHash.OldFullLocalPath, fileHash.FullLocalPath);
                                }

                                Logger.WriteLine(string.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                    _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                break;

                            case FileChangeTypes.DeletedOnServer:
                                if (Helper.IsDirectory(fileHash.FullLocalPath))
                                {
                                    if (Directory.Exists(fileHash.FullLocalPath))
                                    {
                                        Directory.Delete(fileHash.FullLocalPath, true);
                                        Logger.WriteLine(string.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                            _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                    }
                                    else
                                    {
                                        Logger.WriteLine(string.Format("{0}. Fail on dequeue: {1} ==> {2}\n(Directory " + fileHash.FullLocalPath + "doesn't exist)",
                                            _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                    }
                                }
                                else if (File.Exists(fileHash.FullLocalPath))
                                {
                                    fileHash.FileStream.Close();
                                    File.Delete(fileHash.FullLocalPath);
                                    Logger.WriteLine(string.Format("{0}. Succes on dequeue: {1} ==> {2}",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                }
                                else
                                {
                                    Logger.WriteLine(string.Format("{0}. Fail on dequeue: {1} ==> {2}\n(File " + fileHash.FullLocalPath + "doesn't exist)",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                }
                                break;
                        }
                        processed = true;
						
						_changedFilesList.Remove(fileHash);
						if(!processed)
							_changedFilesList.Add(fileHash);

                        Thread.Sleep(500);
                    }
                    else
                    {
	                    On = false;
						return;
                    }
                }
                catch (Exception ex)
                {
                    string exMessage;
                    if (fileHash != null)
                    {
                        exMessage = string.Format("\n!!! EXCEPTION on SyncProcessor:\n\tFileHash:\n{0}\n\tMessage: {1}\n\n\tType: {2}\n\n\tSource: {3}\n\n\tStackTrace:\n {4}\n",
                            fileHash,
                            ex.Message,
                            ex.GetType(),
                            ex.Source,
                            ex.StackTrace);

                        if(fileHash.FileStream != null)
                            fileHash.FileStream.Dispose();

                        if (_changedFilesList != null)
                            _changedFilesList.Remove(fileHash);
                        else
                            exMessage += "\n!!! _changedFilesList was null !!! Why ?";
                    }
                    else
                    {
                        exMessage = "!!! EXCEPTION on SyncProcessor: fileHash was null. HOW ?!?!?!";
                    }

                    Logger.WriteLine(exMessage);
                }
            }
        }
    }
}
