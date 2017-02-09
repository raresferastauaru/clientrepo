using System;
using System.Collections.Generic;
using System.Linq;

using ClientApplication.Models;

namespace ClientApplication.Processors
{
    public class InitialSyncProcessorHelper
    {
        public static List<CustomFileHash> GetProcessedFileHashes(List<CustomFileHash> clientFileHashes, List<CustomFileHash> serverFileHashes)
        {
            var initialSyncFiles = new List<CustomFileHash>();

            var contor = 0;
            var blackList = new List<CustomFileHash>();
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

                            initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnServer,
                                    serverFileHash.RelativePath, serverFileHash.OldRelativePath,
                                    serverFileHash.HashCode, serverFileHash.OldHashCode));
                        }
                        else
                        {
                            str = String.Format("{0}. PUT of file {1} ({2}) (Update on CLIENT)", contor++,
                                                serverFileHash.RelativePath, serverFileHash.HashCode);

                            initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnClient,
                                    serverFileHash.RelativePath, serverFileHash.OldRelativePath,
                                    serverFileHash.HashCode, serverFileHash.OldHashCode));
                        }
                    }
                    else if (serverFileHash.RelativePath != localFileHash.RelativePath &&
                             serverFileHash.HashCode == localFileHash.HashCode)
                    {
                        str = String.Format(localFileHash.RelativePath == serverFileHash.RelativePath ? 
                                            "{0}. RENAME of file {1} to {2} (New name on SERVER) - NOT IMPLEMENTED YET !" 
                                            : 
                                            "{0}. RENAME of file {1} to {2} (New name on CLIENT) - NOT IMPLEMENTED YET !", 
                                            contor++, serverFileHash.OldRelativePath, serverFileHash.RelativePath);
                    }
                    else
                    {
                        str = String.Format("{0}. UNDEFINED scenario on InitialSyncProcessor. Please investigate it!\n{1}. ", contor++, serverFileHash);
                    }
                }
                else
                {
                    str = String.Format("{0}. GET of file {1}({2}) (New file on SERVER).", ++contor,
                                        serverFileHash.RelativePath, serverFileHash.HashCode);

                    initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnServer,
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

            if (clientFileHashes.Count == 0) 
                return initialSyncFiles;
            
            foreach (var clientFileHash in clientFileHashes)
            {
                var str = String.Format("{0}. PUT of file {1}({2}) (New file on CLIENT).", ++contor, clientFileHash.RelativePath, clientFileHash.HashCode);
                Logger.WriteLine(str);

                initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnClient, 
                    clientFileHash.RelativePath, clientFileHash.OldRelativePath,
                    clientFileHash.HashCode, clientFileHash.OldHashCode));
            }

            return initialSyncFiles;
        }
    }
}