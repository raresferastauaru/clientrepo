using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using ClientApplication.Models;

namespace ClientApplication.APIs
{
    public class TcpCommunication : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _networkStream;
		private readonly int bufferSize = 8192; // 1492;

        public TcpCommunication(String hostName, Int32 port)
        {
            try
            {
                _tcpClient = new TcpClient(hostName, port);
                _networkStream = _tcpClient.GetStream();
            }
            catch (ArgumentNullException e)
            {
                throw new Exception("ArgumentNullException: " + e);
            }
            catch (SocketException e)
            {
                throw new Exception("SocketException: " + e);
            }
        }
        public void Dispose()
        {
            _networkStream.Close();
            _tcpClient.Close();
        }

		/* ALL THE FILES WILL HAVE THE PATH LIKE "FOLDER1/FILE.EXT" */

        public bool Get(String fileName)
        {
			// Prepare and send the GET command
            var getCommandBytes = Encoding.Default.GetBytes("GET:" + fileName + ":");
            _networkStream.Write(getCommandBytes, 0, getCommandBytes.Count());
            
            var readMessage = new byte[1024];
            var bytesRead = _networkStream.Read(readMessage, 0, 1024);
            var messageParts = Encoding.Default.GetString(readMessage).Split(':');

			// If we get Acknowledge => the file exists. Beside this, we get the file details.
            if (messageParts[0].Equals("ACKNOWLEDGE") && _networkStream.CanRead)
            {
                Helper.ValidateDirectoryForFile(fileName);
                using (var fileStream = File.Create(Helper.GetLocalPath(fileName)))
                {
                    var offsetSize = (messageParts[0].Length                // Acknowledge
                                      + messageParts[1].Length              // MessageLength
                                      + messageParts[2].Length              // CreationTime
                                      + messageParts[3].Length              // LastWriteTime
                                      + messageParts[4].Length              // IsReadOnly
                                      + 5);                                 // Plus 1 for each ':' contained in readMessage
                    var remainedMessageSize = bytesRead - offsetSize;
					
					// If we have some DataContent in the Acknowledge message we write that in the file.
                    if (remainedMessageSize > 0)
                        fileStream.Write(readMessage, offsetSize, remainedMessageSize);//readMessage.Length - 1);

					// Writing the DataContent in file until we get the entire expected size.
	                var buffer = new byte[bufferSize];
                    var expectedSize = int.Parse(messageParts[1]);
                    while (expectedSize > 0)
                    {
                        var numberOfBytesRead = _networkStream.Read(buffer, 0, buffer.Length);
                        fileStream.Write(buffer, 0, numberOfBytesRead);
                        expectedSize -= numberOfBytesRead;
                    }
                }

                return ChangeFileAttributes(fileName, messageParts[2], messageParts[3], messageParts[4]);
            }
            
			// messageParts[1] should be "Error"
            var message = messageParts[1] ?? "";
            if (Context.InAutoMode)
            {
                Logger.WriteLine("Error message in GET Command: " + message);
                return false;
            }
            throw new Exception(message);
        }

        public bool Put(CustomFileHash customFileHash)
        {
	        var dataBytes = File.ReadAllBytes(customFileHash.FullLocalPath);

			// Prepare and send the PUT command
            var putCommandBytes = Encoding.Default.GetBytes("PUT:" + customFileHash.RelativePath + ":" + dataBytes.Length + ":");
            _networkStream.Write(putCommandBytes, 0, putCommandBytes.Length);
			
			// Processing the response for the PUT command
			string message;
            var readMessage = new byte[1024];
            _networkStream.Read(readMessage, 0, 1024);
            var messageParts = Encoding.ASCII.GetString(readMessage).Split(':');
            if (messageParts[0].Equals("ACKNOWLEDGE"))
            {
				// Sending the entire DataContent
				var buffer = new byte[bufferSize];
	            var fileStream = new FileStream(customFileHash.FullLocalPath, FileMode.Open, FileAccess.Read, FileShare.None);
				var readBytes = 0;
		        while ((readBytes = fileStream.Read(buffer, 0, buffer.Length)) > 0)
		        {
			        _networkStream.Write(dataBytes, 0, readBytes);
		        }
				fileStream.Close();


	            // Processing the response for sent data
                readMessage = new byte[1024];
                _networkStream.Read(readMessage, 0, 1024);
                messageParts = Encoding.ASCII.GetString(readMessage).Split(':');
                if (messageParts[0].Equals("ACKNOWLEDGE"))
                {
                    // Sending the FileHashDetails for the transmited data
                    Logger.WriteLine(customFileHash.GetFileHashDetails());
                    var fileHashDetails = Encoding.Default.GetBytes(customFileHash.GetFileHashDetails());
                    _networkStream.Write(fileHashDetails, 0, fileHashDetails.Length);

					// Processing the response for the FileHashDetails that were sent
                    readMessage = new byte[1024];
                    _networkStream.Read(readMessage, 0, 1024);
                    messageParts = Encoding.ASCII.GetString(readMessage).Split(':');
					if (messageParts[0].Equals("ACKNOWLEDGE"))
					{
						Logger.WriteLine("The FileHash of \"" + customFileHash.RelativePath + "\" was sent successfully.");
						return true;
                    }

					message = messageParts[1];
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
            
            throw new Exception(message);
        }

        public bool Rename(String oldRelativePath, String newRelativePath)
		{
			// Prepare and send the RENAME command
            var renameCommandBytes = Encoding.Default.GetBytes("RENAME:" + oldRelativePath + ":" + newRelativePath + ":");
            _networkStream.Write(renameCommandBytes, 0, renameCommandBytes.Length);

			// Processing the response for the requested rename
            var readMessage = new byte[1024];
            var bytesRead = _networkStream.Read(readMessage, 0, 1024);
            var messageParts = Encoding.ASCII.GetString(readMessage).Split(':');
            if (messageParts[0].Equals("ACKNOWLEDGE") && _networkStream.CanRead)
            {
                readMessage = new byte[1024];
                _networkStream.Read(readMessage, 0, 1024);
                messageParts = Encoding.ASCII.GetString(readMessage).Split(':');
                if (messageParts[0].Equals("ACKNOWLEDGE"))
                {
                    Logger.WriteLine("File " + oldRelativePath + " was renamed to " + newRelativePath + " successfully.");
                    return true;
                }
            }

            var errorMessage = messageParts[1];
            throw new Exception(errorMessage);
        }

        public bool Delete(String relativePath)
		{
			// Prepare and send the DELETE command
            var deleteCommandBytes = Encoding.Default.GetBytes("DELETE:" + relativePath + ":");
            _networkStream.Write(deleteCommandBytes, 0, deleteCommandBytes.Length);

            var readMessage = new byte[1024];
            var bytesRead = _networkStream.Read(readMessage, 0, 1024);
            var messageParts = Encoding.ASCII.GetString(readMessage).Split(':');

            if (messageParts[0].Equals("ACKNOWLEDGE") && _networkStream.CanRead)
            {
                return true;
            }

            var errorMessage = messageParts[1];
            throw new Exception(errorMessage);
        }

        public bool Mkdir(String folderName)
		{
			// Prepare and send the MKDIR command
            var newFolderCommandBytes = Encoding.Default.GetBytes("MKDIR:" + folderName + ":");
            _networkStream.Write(newFolderCommandBytes, 0, newFolderCommandBytes.Length);

            var readMessage = new byte[1024];
            var bytesRead = _networkStream.Read(readMessage, 0, 1024);
            var messageParts = Encoding.ASCII.GetString(readMessage).Split(':');

            if (messageParts[0].Equals("ACKNOWLEDGE") && _networkStream.CanRead)
            {
                return true;
            }

            var errorMessage = messageParts[1];
            throw new Exception(errorMessage);
        }

        public void Kill()
        {
            var killCommandBytes = Encoding.Default.GetBytes("KILL:");
            _networkStream.Write(killCommandBytes, 0, killCommandBytes.Length);
        }

        public List<CustomFileHash> GetAllFileHashes()
		{
			// Prepare and send the GETFileHashes command
            var getFileHashesCommandBytes = Encoding.Default.GetBytes("GETFileHashes:" + Context.CurrentUser + ":");
            _networkStream.Write(getFileHashesCommandBytes, 0, getFileHashesCommandBytes.Length);

			// Reading all FileHashes string
            var memoryStream = new MemoryStream();
            var myReadBuffer = new byte[bufferSize];
            do
            {
                var numberOfBytesRead = _networkStream.Read(myReadBuffer, 0, myReadBuffer.Length);
                memoryStream.Write(myReadBuffer, 0, numberOfBytesRead);
            } while (_networkStream.DataAvailable);

            var serverFileHashes = new List<CustomFileHash>();
            var receivedData = Encoding.Default.GetString(memoryStream.GetBuffer());

			// If we didn't got errors we start processing the received FileHashes
            if (!receivedData.StartsWith("Error:"))
            {
                var processedData = receivedData.Split('|');
                processedData = processedData.Take(processedData.Count() - 1).Distinct().ToArray();

                foreach (var data in processedData)
                {
                    var splits = data.Split(':');
                    serverFileHashes.Add(new CustomFileHash(FileChangeTypes.None, splits[0], splits[1], int.Parse(splits[2]), int.Parse(splits[3])));
                }
            }
            else
            {
                var message = "Error message in GetAllFileHashes Command: " + receivedData.Split(':')[1];

                if (Context.InAutoMode)
                    Logger.WriteLine(message);
                else
                    throw new Exception(message);
            }

            return serverFileHashes;
        }



        private bool ChangeFileAttributes(string fileName, string creationTimeTicks, string lastWriteTimeTicks, string isReadOnlyString)
        {
            //try
            var fullPath = Helper.GetLocalPath(fileName);
            var creationTime = new DateTime(long.Parse(creationTimeTicks));
            var lastWriteTime = new DateTime(long.Parse(lastWriteTimeTicks));
            var isReadOnly = bool.Parse(isReadOnlyString);

            File.SetCreationTime(fullPath, creationTime);
            File.SetLastWriteTime(fullPath, lastWriteTime);
            
            var fileAttributes = File.GetAttributes(fullPath);
            if (isReadOnly)
                fileAttributes |= FileAttributes.ReadOnly;

            File.SetAttributes(fullPath, fileAttributes);
            //catch(Exception ex) { return false; }
            return true;
        }
    }
}