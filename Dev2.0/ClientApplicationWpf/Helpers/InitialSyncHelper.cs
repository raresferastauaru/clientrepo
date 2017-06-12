using ClientApplicationWpf.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClientApplicationWpf.Helpers
{
    public class InitialSyncHelper
    {
        public static List<CustomFileHash> GetProcessedFileHashes(List<CustomFileHash> clientFileHashes, List<CustomFileHash> serverFileHashes)
        {
            var initialSyncFiles = new List<CustomFileHash>();

            var contor = 0;
            var blackList = new List<CustomFileHash>();
            foreach (var serverFileHash in serverFileHashes)
            {
                var localFileHash = clientFileHashes.FirstOrDefault(lfh => lfh.HashCode.Equals(serverFileHash.HashCode))
                                    ??
                                    clientFileHashes.FirstOrDefault(lfh => lfh.HashCode.Equals(serverFileHash.OldHashCode));

                //var localFileHash = clientFileHashes.FirstOrDefault(lfh => lfh.RelativePath.Equals(serverFileHash.RelativePath))
                //					??
                //					clientFileHashes.FirstOrDefault(lfh => lfh.RelativePath.Equals(serverFileHash.OldRelativePath));

                string message;
                if (localFileHash != null)
                {
                    if (serverFileHash.RelativePath == localFileHash.RelativePath &&
                        serverFileHash.HashCode == localFileHash.HashCode)
                    {
                        if (serverFileHash.IsDeleted)
                        {
                            message = string.Format("{0}. Actualizat: {1}. (Deja șters pe Server)", contor++, localFileHash.RelativePath);

                            initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.DeletedOnServer,
                                serverFileHash.RelativePath, serverFileHash.OldRelativePath,
                                serverFileHash.HashCode, serverFileHash.OldHashCode));
                        }
                        else
                        {
                            message = string.Format("{0}. Actualizat: {1}.", contor++, localFileHash.RelativePath);
                        }
                    }
                    else if (serverFileHash.RelativePath == localFileHash.RelativePath &&
                             serverFileHash.HashCode != localFileHash.HashCode)
                    {
                        if (localFileHash.HashCode == serverFileHash.OldHashCode)
                        {
                            message = string.Format("{0}. Obține: {1}. (Actualizare pe Server)", contor++, serverFileHash.RelativePath);

                            initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnServer,
                                serverFileHash.RelativePath, serverFileHash.OldRelativePath,
                                serverFileHash.HashCode, serverFileHash.OldHashCode));
                        }
                        else
                        {
                            message = string.Format("{0}. Pune: {1}. (Actualizare pe Client)", contor++, serverFileHash.RelativePath);

                            initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnClient,
                                localFileHash.RelativePath, localFileHash.OldRelativePath,
                                localFileHash.HashCode, localFileHash.OldHashCode));
                        }
                    }
                    else if (serverFileHash.RelativePath != localFileHash.RelativePath &&
                             serverFileHash.HashCode == localFileHash.HashCode)
                    {
                        if (localFileHash.RelativePath == serverFileHash.OldRelativePath)
                        {
                            message = string.Format("{0}. Redenumește: {1} la {2}. (Nume nou pe Server)", contor++,
                                serverFileHash.OldRelativePath, serverFileHash.RelativePath);

                            initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.RenamedOnServer,
                                serverFileHash.RelativePath, serverFileHash.OldRelativePath,
                                serverFileHash.HashCode, serverFileHash.OldHashCode));
                            blackList.Add(localFileHash);
                        }
                        else
                        {
                            if (!serverFileHash.IsDeleted)
                            {
                                message = string.Format("{0}. Redenumește: {1} la {2}. (Nume nou pe Client)", contor++,
									serverFileHash.RelativePath, localFileHash.RelativePath);

                                initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.RenamedOnClient,
                                    localFileHash.RelativePath, serverFileHash.RelativePath,
                                    serverFileHash.HashCode, serverFileHash.OldHashCode));
                            }
                            else
							{
								message = string.Format("{0}. Actualizat: {1}. (Deja șters pe Server)", contor++, serverFileHash.RelativePath);
                                // add a deleted on server change... just to make sure that the file is deleted on client too ?
                            }

                            blackList.Add(localFileHash);
                        }
                    }
                    else if (serverFileHash.RelativePath != localFileHash.RelativePath &&
                             serverFileHash.HashCode != localFileHash.HashCode)
                    {
                        if(serverFileHash.OldHashCode == localFileHash.HashCode && 
                            serverFileHash.IsDeleted)
						{
							message = string.Format("{0}. Actualizat: {1}. (Deja șters pe Server)", contor++, serverFileHash.RelativePath);

                            blackList.Add(localFileHash);
                        }
                        else
                        {
                            message = string.Format("{0}. UNDEFINED: InitialSyncProcessor. Please investigate it!\nServerFileHash: {1}\nClientFileHash: {2}\n.",
                                contor++,
                                serverFileHash, localFileHash);
							blackList.Add(localFileHash);
						}
                    }
                    else
                    {
                        message = string.Format("{0}. UNDEFINED: InitialSyncProcessor. Please investigate it!\nServerFileHash: {1}\nClientFileHash: {2}\n.",
                                contor++,
                                serverFileHash, localFileHash);
                        blackList.Add(localFileHash);
                    }
                }
                else
                {
                    if (!serverFileHash.IsDeleted)
                    {
                        message = string.Format("{0}. Obține: {1}. (Fișier nou pe Server)", ++contor, serverFileHash.RelativePath);

                        initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnServer,
                            serverFileHash.RelativePath, serverFileHash.OldRelativePath,
                            serverFileHash.HashCode, serverFileHash.OldHashCode));
                    }
                    else
                    {
						message = string.Format("{0}. Actualizat: {1}. (Deja șters pe Server)", contor++,
                            serverFileHash.RelativePath, serverFileHash.GetFileHashBasicDetails());

                        if (File.Exists(Helper.GetLocalPath(serverFileHash.RelativePath)))
                        {
                            File.Delete(Helper.GetLocalPath(serverFileHash.RelativePath));
							message.TrimEnd(')');
                            message += " și pe Client de asemenea)";
                        }
                    }
                }

                blackList.Add(serverFileHash);
                Logger.WriteLine(message);
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
                Logger.WriteLine(string.Format("{0}. Pune: {1}. (Fișier nou pe Client)", 
					++contor, clientFileHash.RelativePath, clientFileHash.HashCode));

                initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnClient,
                    clientFileHash.RelativePath, clientFileHash.OldRelativePath,
                    clientFileHash.HashCode, clientFileHash.OldHashCode));
            }

            return initialSyncFiles;
        }
    }
}
