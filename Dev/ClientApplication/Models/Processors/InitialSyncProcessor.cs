using System;
using System.Collections.Generic;
using System.Linq;
using ClientApplication.Models.FileHashes;

namespace ClientApplication.Models.Processors
{
    public class InitialSyncProcessor
    {
        public List<InitialSyncFileHash> GetProcessedFileHashes(List<InitialSyncFileHash> clientFileHashes, List<InitialSyncFileHash> serverFileHashes)
        {
            var initialSyncFiles = new List<InitialSyncFileHash>();

            var contor = 0;
            var blackList = new List<InitialSyncFileHash>();
            foreach (var serverFileHash in serverFileHashes)
            {
                var localFileHash = clientFileHashes.FirstOrDefault(lfh => lfh.RelativePath.Equals(serverFileHash.RelativePath));
                string str;

                if (localFileHash != null)
                {
                    if (serverFileHash.RelativePath == localFileHash.RelativePath &&
                        serverFileHash.HashCode == localFileHash.HashCode)
                    {
                        str = String.Format("{0}. File {1} is up to date !", contor++, localFileHash.RelativePath);
                    }
                    else if (serverFileHash.RelativePath == localFileHash.RelativePath &&
                             serverFileHash.HashCode != localFileHash.HashCode)
                    {
                        if (localFileHash.HashCode == serverFileHash.OldHashCode)
                        {
                            str = String.Format("{0}. GET of file {1} ({2}) (Update on SERVER)", contor++,
                                serverFileHash.RelativePath, serverFileHash.HashCode);

                            initialSyncFiles.Add(
                                new InitialSyncFileHash(FileChangeTypes.ChangedOnServer,
                                    serverFileHash.RelativePath, serverFileHash.OldRelativePath,
                                    serverFileHash.HashCode, serverFileHash.OldHashCode));
                        }
                        else
                        {
                            str = String.Format("{0}. PUT of file {1} ({2}) (Update on CLIENT)", contor++,
                                serverFileHash.RelativePath, serverFileHash.HashCode);

                            initialSyncFiles.Add(
                                new InitialSyncFileHash(FileChangeTypes.ChangedOnClient,
                                    serverFileHash.RelativePath, serverFileHash.OldRelativePath,
                                    serverFileHash.HashCode, serverFileHash.OldHashCode));
                        }
                    }
                    else if (serverFileHash.RelativePath != localFileHash.RelativePath &&
                             serverFileHash.HashCode == localFileHash.HashCode)
                    {
                        if (localFileHash.RelativePath == serverFileHash.RelativePath)
                        {
                            str = String.Format("{0}. RENAME of file {1} to {2} (New name on SERVER) - NOT IMPLEMENTED YET !", contor++,
                                serverFileHash.OldRelativePath, serverFileHash.RelativePath);
                        }
                        else
                        {
                            str = String.Format("{0}. RENAME of file {1} to {2} (New name on CLIENT) - NOT IMPLEMENTED YET !", contor++,
                                serverFileHash.OldRelativePath, serverFileHash.RelativePath);
                        }
                    }
                    else
                    {
                        {
                            str = String.Format("{0}. UNDEFINED: {1}. Please investigate it !!!", contor++,
                                serverFileHash.RelativePath);
                        }
                    }
                }
                else
                {
                    str = String.Format("{0}. GET of file {1}({2}) (New file on SERVER).", ++contor,
                        serverFileHash.RelativePath, serverFileHash.HashCode);

                    initialSyncFiles.Add(
                        new InitialSyncFileHash(FileChangeTypes.ChangedOnServer,
                            serverFileHash.RelativePath, serverFileHash.OldRelativePath,
                            serverFileHash.HashCode, serverFileHash.OldHashCode));
                }

                blackList.Add(serverFileHash);
                Logger.WriteLine(str);
            }

            foreach (var sfh in blackList)
            {
                var clientFileHash = clientFileHashes.FirstOrDefault(cfh => cfh.RelativePath.Equals(sfh.RelativePath));
                if (clientFileHash != null)
                    clientFileHashes.Remove(clientFileHash);
            }
            if (clientFileHashes.Count > 0)
            {
                foreach (var clientFileHash in clientFileHashes)
                {
                    var str = String.Format("{0}. PUT of file {1}({2}) (New file on CLIENT).", ++contor, clientFileHash.RelativePath, clientFileHash.HashCode);
                    Logger.WriteLine(str);

                    initialSyncFiles.Add(
                        new InitialSyncFileHash(FileChangeTypes.ChangedOnClient, 
                            clientFileHash.RelativePath, clientFileHash.OldRelativePath,
                            clientFileHash.HashCode, clientFileHash.OldHashCode));
                }
            }

            return initialSyncFiles;
        }
    }
}
