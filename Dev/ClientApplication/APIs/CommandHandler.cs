using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ClientApplication.Models;
using System.Threading;

namespace ClientApplication.APIs
{
	public class CommandHandler: IDisposable
	{
		private TcpCommunication _tcpCommunication;

		public CommandHandler(TcpCommunication tcpCommunication)
		{
			_tcpCommunication = tcpCommunication;
		}
		public void Dispose()
		{
			_tcpCommunication = null;
		}

        public async Task<bool> Get(string relativePath)
        {
			// Prepare and send the GET command
			var getCommandBytes = Encoding.UTF8.GetBytes("GET:" + relativePath + ":");
			_tcpCommunication.SendCommand(getCommandBytes, 0, getCommandBytes.Count());

			await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
			var readMessage = _tcpCommunication.CommandResponseBuffer.Receive();
			var messageParts = Encoding.UTF8.GetString(readMessage).Split(':');

			// If we get Acknowledge => the file exists. Beside this, we get the file details.
			if (messageParts[0].Equals("ACKNOWLEDGE"))
			{
				Helper.ValidateDirectoryForFile(relativePath);

                using (var fileStream = File.Create(Helper.GetLocalPath(relativePath)))
				{
                    if (int.Parse(messageParts[1]) > 0)
                    {
                        var offsetSize = (messageParts[0].Length            // Acknowledge
                                        + messageParts[1].Length              // MessageLength
                                        + messageParts[2].Length              // CreationTime
                                        + messageParts[3].Length              // LastWriteTime
                                        + messageParts[4].Length              // IsReadOnly
                                        + 5);                                 // Plus 1 for each ':' contained in readMessage

                        var fullLength = offsetSize + messageParts[5].Length - 1;

                        // If we have some DataContent in the Acknowledge message we write that in the file.
                        var remainedMessageSize = fullLength - offsetSize;
                        var expectedSize = int.Parse(messageParts[1]);
                        if (remainedMessageSize > 0)
                        {
                            fileStream.Write(readMessage, offsetSize, remainedMessageSize);
                            expectedSize -= remainedMessageSize;
                        }

                        // Writing the DataContent in file until we get the entire expected size.
                        await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
                        while (expectedSize > 0)
                        {
                            var buffer = _tcpCommunication.CommandResponseBuffer.Receive();
                            fileStream.Write(buffer, 0, buffer.Length);
                            expectedSize -= buffer.Length;
                        }
                    }

                    fileStream.Close();
				}

				// Applying the file details.
				return Helper.ChangeFileAttributes(relativePath, messageParts[2], messageParts[3], messageParts[4]);
			}

			if (!Context.InAutoMode) throw new Exception(messageParts[1]);
			Logger.WriteLine("Error message in GET Command: " + messageParts[1]);
			return false;
		}

		public async Task<bool> Put(CustomFileHash customFileHash)
		{
			// Prepare and send the PUT command
			var fileSize = new FileInfo(customFileHash.FullLocalPath).Length;
			var putCommandBytes = Encoding.UTF8.GetBytes("PUT:" + customFileHash.RelativePath + ":" + customFileHash.HashCode + ":" + fileSize + ":");
			_tcpCommunication.SendCommand(putCommandBytes, 0, putCommandBytes.Length);

			// Processing the response for the PUT command
			string message;
			await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
			var readMessage = _tcpCommunication.CommandResponseBuffer.Receive();
			var messageParts = Encoding.UTF8.GetString(readMessage).Split(':');
			if (messageParts[0].Equals("ACKNOWLEDGE"))
			{
				// Sending the entire DataContent
				if (fileSize != 0)
				{
					var buffer = new byte[Helper.BufferSize];
                    using (var fileStream = new FileStream(customFileHash.FullLocalPath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        int readBytes;
                        while ((readBytes = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            _tcpCommunication.SendCommand(buffer, 0, readBytes);
                        }
                        fileStream.Close();
                    }
                }

				// Processing the response for sent data
				await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
				readMessage = _tcpCommunication.CommandResponseBuffer.Receive();
				messageParts = Encoding.UTF8.GetString(readMessage).Split(':');
				if (messageParts[0].Equals("ACKNOWLEDGE"))
				{
					// Sending the FileHashDetails for the transmited data
					Logger.WriteLine(customFileHash.GetFileHashDetails());
					var fileHashDetails = Encoding.UTF8.GetBytes(customFileHash.GetFileHashDetails());
					_tcpCommunication.SendCommand(fileHashDetails, 0, fileHashDetails.Length);

					// Processing the response for the FileHashDetails that were sent
					await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
					readMessage = _tcpCommunication.CommandResponseBuffer.Receive();
					messageParts = Encoding.UTF8.GetString(readMessage).Split(':');
					if (messageParts[0].Equals("ACKNOWLEDGE"))
					{
						Logger.WriteLine("The FileHash of \"" + customFileHash.RelativePath + "\" was sent successfully.");
						return true;
					}
					else
					{
						message = messageParts[1];
					}
				}
				else
				{
					message = messageParts[1];
				}
			}
			else
			{
				message = messageParts[1];
			}

			if (!Context.InAutoMode) throw new Exception(message);
			Logger.WriteLine("Error message in PUT Command: " + message);
			return false;
		}

		public async Task<bool> Rename(string oldRelativePath, string newRelativePath)
		{
			// Prepare and send the RENAME command
			var renameCommandBytes = Encoding.UTF8.GetBytes("RENAME:" + oldRelativePath + ":" + newRelativePath + ":");
			_tcpCommunication.SendCommand(renameCommandBytes, 0, renameCommandBytes.Length);

			// Processing the response for the requested rename
			string message;
			await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
			var readMessage = _tcpCommunication.CommandResponseBuffer.Receive();
			var messageParts = Encoding.UTF8.GetString(readMessage).Split(':');
			if (messageParts[0].Equals("ACKNOWLEDGE"))
			{
				await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
				readMessage = _tcpCommunication.CommandResponseBuffer.Receive();
				messageParts = Encoding.UTF8.GetString(readMessage).Split(':');
				if (messageParts[0].Equals("ACKNOWLEDGE"))
				{
					Logger.WriteLine("File " + oldRelativePath + " was renamed to " + newRelativePath + " successfully.");
					return true;
				}
				else
				{
					message = messageParts[1];
				}
			}
			else
			{
				message = messageParts[1];
			}

			if (!Context.InAutoMode) throw new Exception(message);
			Logger.WriteLine("Error message in RENAME Command: " + message);
			return false;
		}

		public async Task<bool> Delete(string relativePath)
		{
			// Prepare and send the DELETE command
			var deleteCommandBytes = Encoding.UTF8.GetBytes("DELETE:" + relativePath + ":");
			_tcpCommunication.SendCommand(deleteCommandBytes, 0, deleteCommandBytes.Length);

            Thread.Sleep(250);                                                                      //  ?!!??!?!?!

			await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
			var readMessage = _tcpCommunication.CommandResponseBuffer.Receive();
			var messageParts = Encoding.UTF8.GetString(readMessage).Split(':');

			if (messageParts[0].Equals("ACKNOWLEDGE"))
				return true;

			if (!Context.InAutoMode) throw new Exception(messageParts[1]);
			Logger.WriteLine("Error message in DELETE Command: " + messageParts[1]);
			return false;
		}

		public async Task<bool> Mkdir(string folderName)
		{
			// Prepare and send the MKDIR command
			var newFolderCommandBytes = Encoding.UTF8.GetBytes("MKDIR:" + folderName + ":");
			_tcpCommunication.SendCommand(newFolderCommandBytes, 0, newFolderCommandBytes.Length);

			await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
			var readMessage = _tcpCommunication.CommandResponseBuffer.Receive();
			var messageParts = Encoding.UTF8.GetString(readMessage).Split(':');

			if (messageParts[0].Equals("ACKNOWLEDGE"))
				return true;

			if (!Context.InAutoMode) throw new Exception(messageParts[1]);
			Logger.WriteLine("Error message in MKDIR Command: " + messageParts[1]);
			return false;
		}

		public void Kill()
		{
			var killCommandBytes = Encoding.UTF8.GetBytes("KILL:");
			_tcpCommunication.SendCommand(killCommandBytes, 0, killCommandBytes.Length);
		}

		public async Task<List<CustomFileHash>> GetAllFileHashes()
		{
			// Prepare and send the GETFileHashes command
			var getFileHashesCommandBytes = Encoding.UTF8.GetBytes("GETFileHashes:" + Context.CurrentUser + ":");
			_tcpCommunication.SendCommand(getFileHashesCommandBytes, 0, getFileHashesCommandBytes.Length);

			// Reading all FileHashes string
			var memoryStream = new MemoryStream();
			while (await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync())
			{
				var buffer = _tcpCommunication.CommandResponseBuffer.Receive();
				memoryStream.Write(buffer, 0, buffer.Length);
			}

			var serverFileHashes = new List<CustomFileHash>();
			var receivedData = Encoding.UTF8.GetString(memoryStream.GetBuffer());

            // If we didn't got errors we start processing the received FileHashes
            if (!receivedData.StartsWith("Error:"))
            {
                var processedData = receivedData.Split('|');
                processedData = processedData.Take(processedData.Count() - 1).Distinct().ToArray();

                foreach (var data in processedData)
                {
                    var splits = data.Split(':');
                    serverFileHashes.Add(new CustomFileHash(FileChangeTypes.None, splits[0], splits[1], int.Parse(splits[2]), int.Parse(splits[3]), splits[4].Equals("1")));
                }
            }
            else
            {
                if (!Context.InAutoMode)
                    throw new Exception(receivedData.Split(':')[1]);
                Logger.WriteLine("Error message in GetAllFileHashes Command: " + receivedData.Split(':')[1]);
            }

            return serverFileHashes;
        }
	}
}
