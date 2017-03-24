using System.Collections.Generic;
using System.IO;
using System.Linq;

using ClientApplication.Models;

namespace ClientApplication.Processors
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
					        message = string.Format("{0}. DELETED ON SERVER: {1}! --- {2}", contor++, localFileHash.RelativePath,
						        localFileHash.GetFileHashBasicDetails());

					        initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.DeletedOnServer,
						        serverFileHash.RelativePath, serverFileHash.OldRelativePath,
						        serverFileHash.HashCode, serverFileHash.OldHashCode));
				        }
				        else
				        {
					        message = string.Format("{0}. UP TO DATE: {1}! --- {2}", contor++, localFileHash.RelativePath,
						        localFileHash.GetFileHashBasicDetails());
				        }
			        }
			        else if (serverFileHash.RelativePath == localFileHash.RelativePath &&
			                 serverFileHash.HashCode != localFileHash.HashCode)
			        {
				        if (localFileHash.HashCode == serverFileHash.OldHashCode)
				        {
					        message = string.Format("{0}. GET: {1} ({2}) (Update on SERVER)", contor++,
						        serverFileHash.RelativePath, serverFileHash.HashCode);

					        initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnServer,
						        serverFileHash.RelativePath, serverFileHash.OldRelativePath,
						        serverFileHash.HashCode, serverFileHash.OldHashCode));
				        }
				        else
				        {
					        message = string.Format("{0}. PUT: {1} ({2}) (Update on CLIENT)", contor++,
						        serverFileHash.RelativePath, serverFileHash.HashCode);

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
					        message = string.Format("{0}. RENAME: {1} to {2} (New name on SERVER)", contor++,
						        serverFileHash.OldRelativePath, serverFileHash.RelativePath);

					        initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.RenamedOnServer,
						        serverFileHash.RelativePath, serverFileHash.OldRelativePath,
						        serverFileHash.HashCode, serverFileHash.OldHashCode));
                            blackList.Add(localFileHash);
                        }
				        else
                        { 
					        message = string.Format("{0}. RENAME: {1} to {2} (New name on CLIENT)", contor++,
						        serverFileHash.RelativePath, localFileHash.RelativePath);

					        initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.RenamedOnClient,
						        localFileHash.RelativePath, serverFileHash.RelativePath,
                                serverFileHash.HashCode, serverFileHash.OldHashCode));
                            blackList.Add(localFileHash);

                            /// WHAT HASHCODES SHOULD WE SEND ?!?!serverFileHash
                            /// how is the Changed & Renamed file handled ?!?!?!
                        }
			        }
			        else
			        {
				        message = string.Format("{0}. UNDEFINED: InitialSyncProcessor. Please investigate it!\nServerFileHash: {1}\nClientFileHash: {2}\n.",
						        contor++,
						        serverFileHash, clientFileHashes);
			        }
		        }
		        else
		        {
			        if (!serverFileHash.IsDeleted)
			        {
				        message = string.Format("{0}. GET: {1} ({2}) (New file on SERVER).", ++contor,
					        serverFileHash.RelativePath, serverFileHash.HashCode);

				        initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnServer,
					        serverFileHash.RelativePath, serverFileHash.OldRelativePath,
					        serverFileHash.HashCode, serverFileHash.OldHashCode));
			        }
			        else
			        {
				        message = string.Format("{0}. UP TO DATE: {1}! --- {2} --- Already DELETED on server.", contor++,
					        serverFileHash.RelativePath,
					        serverFileHash.GetFileHashBasicDetails());

				        if (File.Exists(Helper.GetLocalPath(serverFileHash.RelativePath)))
				        {
					        File.Delete(Helper.GetLocalPath(serverFileHash.RelativePath));
					        message += " DELETED on client too.";
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
                var str = string.Format("{0}. PUT: {1}({2}) (New file on CLIENT).", ++contor, clientFileHash.RelativePath, clientFileHash.HashCode);
                Logger.WriteLine(str);

                initialSyncFiles.Add(new CustomFileHash(FileChangeTypes.ChangedOnClient, 
                    clientFileHash.RelativePath, clientFileHash.OldRelativePath,
                    clientFileHash.HashCode, clientFileHash.OldHashCode));
            }

            return initialSyncFiles;
        }
    }
}