using ClientApplicationWpf.Model;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClientApplicationWpf.APIs
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
            if (_commandHandler != null)
                _commandHandler.Dispose();

            _changedFilesList = null;
        }

        public void AddChangedFile(CustomFileHash customFileHash)
        {
            _changedFilesList.Add(customFileHash);
        }

        //public bool InProcessingList(string relativePath, FileChangeTypes changeType)
        //{
        //    return _changedFilesList.Any(f => f.RelativePath.Equals(relativePath) && f.ChangeType == changeType);
        //}

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
                                Logger.WriteLine(string.Format("{0}. Succes la ștergerea din coadă: {1}. (Trimis pe Server)",
                                    _dequeuedFilesCounter++, fileHash.RelativePath));
                                break;

                            case FileChangeTypes.ChangedOnClient:
                                await _commandHandler.Put(fileHash);
                                Logger.WriteLine(string.Format("{0}. Succes la ștergerea din coadă: {1}. (Trimis pe Server)",
                                    _dequeuedFilesCounter++, fileHash.RelativePath));
                                break;

                            case FileChangeTypes.RenamedOnClient:
                                await _commandHandler.Rename(fileHash);
                                Logger.WriteLine(string.Format("{0}. Succes la ștergerea din coadă: {1} redenumit la {2}. (Redenumit pe Client)",
                                    _dequeuedFilesCounter++, fileHash.OldRelativePath, fileHash.RelativePath));
                                break;

                            case FileChangeTypes.DeletedOnClient:
                                await _commandHandler.Delete(fileHash);
                                Logger.WriteLine(string.Format("{0}. Succes la ștergerea din coadă: {1}. (Șters pe Server)",
                                    _dequeuedFilesCounter++, fileHash.RelativePath));
                                break;


                            // Changes on SERVER
                            case FileChangeTypes.CreatedOnServer:
                                //if (Helper.IsDirectory(fileHash.FullLocalPath))
                                //{
                                Directory.CreateDirectory(fileHash.FullLocalPath);
                                Logger.WriteLine(string.Format("{0}. Succes la ștergerea din coadă: {1}. (Creat pe Server)",
                                    _dequeuedFilesCounter++, fileHash.RelativePath));
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
                                    Logger.WriteLine(string.Format("{0}. Succes la ștergerea din coadă: {1} (Obține de pe server)",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                else
                                    Logger.WriteLine(string.Format("{0}. Eșec la ștergerea din coadă: {1}. (Obține  de pe Server)",
                                        _dequeuedFilesCounter++, fileHash.RelativePath));
                                break;

                            case FileChangeTypes.RenamedOnServer:
                                if (Helper.IsDirectory(fileHash.OldFullLocalPath))
                                    Directory.Move(fileHash.OldFullLocalPath, fileHash.FullLocalPath);
                                else
                                    File.Move(fileHash.OldFullLocalPath, fileHash.FullLocalPath);

                                Logger.WriteLine(string.Format("{0}. Succes la ștergerea din coadă: {1}. (Redenumit pe Server)",
                                    _dequeuedFilesCounter++, fileHash.RelativePath));
                                break;

                            case FileChangeTypes.DeletedOnServer:
                                if (Helper.IsDirectory(fileHash.FullLocalPath))
                                {
                                    if (Directory.Exists(fileHash.FullLocalPath))
                                    {
                                        Directory.Delete(fileHash.FullLocalPath, true);
                                        Logger.WriteLine(string.Format("{0}. Succes la ștergerea din coadă: {1} ==> {2}",
                                            _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                    }
                                    else
                                    {
                                        Logger.WriteLine(string.Format("{0}. Eșec la ștergerea din coadă: {1} (Stergere pe Server: Directorul {3} nu există)",
                                            _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType, fileHash.FullLocalPath));
                                    }
                                }
                                else if (File.Exists(fileHash.FullLocalPath))
                                {
                                    fileHash.FileStream.Close();
                                    File.Delete(fileHash.FullLocalPath);
                                    Logger.WriteLine(string.Format("{0}. Succes la ștergerea din coadă: {1} ==> {2}",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType));
                                }
                                else
                                {
                                    Logger.WriteLine(string.Format("{0}. Eșec la ștergerea din coadă: {1}. (Stergere pe Server: Fișierul {3} nu există)",
                                        _dequeuedFilesCounter++, fileHash.RelativePath, fileHash.ChangeType, fileHash.FullLocalPath));
                                }
                                break;
                        }
                        processed = true;

                        _changedFilesList.Remove(fileHash);
                        if (!processed)
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
                        exMessage = string.Format("Excepție la Procesorul de Sincronizare:" +
							"\n\tFileHash:\n{0}\n\tMessage: {1}\n\n\tType: {2}\n\n\tSource: {3}\n\n\tStackTrace:\n {4}\n",
                            fileHash, ex.Message, ex.GetType(), ex.Source, ex.StackTrace);

                        if (fileHash.FileStream != null)
                            fileHash.FileStream.Dispose();

                        if (_changedFilesList != null)
                            _changedFilesList.Remove(fileHash);
                        else
                            exMessage += "\n!!! _listăFișiereSchimbate a fost NULL !!! De ce?";
                    }
                    else
                    {
                        exMessage = "Excepție la Procesorul de Sincronizare: înregistrareFișier a fost NULL !!! Cum ?!";
                    }

                    Logger.WriteLine(exMessage);
                }
            }
        }
    }

}
