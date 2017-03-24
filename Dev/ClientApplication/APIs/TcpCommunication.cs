using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;
using ClientApplication.Models;
using System.Threading;

namespace ClientApplication.APIs
{
    public class TcpCommunication : IDisposable
    {
        private byte byteColon = Encoding.UTF8.GetBytes(":")[0];
        private byte byteEmpty = Encoding.UTF8.GetBytes("\0")[0];
        private TcpClient _tcpClient;
		private NetworkStream _networkStream;

		public BufferBlock<byte[]> CommandResponseBuffer;
	    public ThreadSafeList<CustomFileHash> ChangedFilesList;

	    public TcpCommunication(TcpClient tcpClient)
	    {
		    try
		    {
			    _tcpClient = tcpClient;
                _networkStream = tcpClient.GetStream();
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

		public TcpCommunication(TcpClient tcpClient, BufferBlock<byte[]> commandResponseBuffer, ThreadSafeList<CustomFileHash> changedFilesList)
	    {
		    try
			{
				_tcpClient = tcpClient;
                _networkStream = tcpClient.GetStream();
				CommandResponseBuffer = commandResponseBuffer;
			    ChangedFilesList = changedFilesList;

	            Task.Factory.StartNew(AsyncReading());
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
			_networkStream.Dispose();
			_tcpClient.Close();

			_tcpClient = null;
			CommandResponseBuffer = null;
			ChangedFilesList = null;
		}


	    private Action AsyncReading()
	    {
			return () =>
		    {
			    while (true)
			    {
				    try
				    {
                        //Reading all the available data on the network stream
					    var buffer = new byte[Helper.BufferSize];
					    var readBytes = _networkStream.Read(buffer, 0, Helper.BufferSize);
					    buffer = buffer.Take(readBytes).ToArray();

					    var readData = Encoding.UTF8.GetString(buffer, 0, readBytes);
					    var splitedData = readData.Split(':').ToList();
                        //File.AppendAllText(@"C:\Users\rares\Desktop\TcpCommunicationUTF8.txt", "RESPONSE: " + readData + "\n");

                        var pushNotificationMessage = splitedData.Any(s => s.Equals("PUSHNOTIFICATION"));
					    if (pushNotificationMessage)
					    {
						    ManagePushNotificationMessage(splitedData, ref buffer);
					    }

					    var eocrMessage = splitedData.Any(s => s.Equals("EOCR"));
                        if (eocrMessage)
                        {
                            ManageEocrMessage(splitedData, buffer);
                        }
                        else if (buffer.Length > 0)
                        {
                            if (buffer.Length == 1 && buffer[0].Equals(byteEmpty))
                            {
                                CommandResponseBuffer.Complete();
                                CommandResponseBuffer = new BufferBlock<byte[]>();
                            }
                            else
                            {
                                CommandResponseBuffer.Post(buffer);
                            }
                        }
				    }
				    catch (ObjectDisposedException)
					{
						// Thread is async and it tries to use an disposed object.
						return;
					}
				    catch (IOException)
					{
						// Network stream throws this exception becouse it tries to access a broken network stream.
						return;
					}
				    catch (Exception ex)
				    {
					    var str = "Source: " + ex.Source + "\nMessage: " + ex.Message + "\nStackTrace: " + ex.StackTrace;
						MessageBox.Show(str, @"AsyncReading - Exception - " + ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
				    }
			    }
		    };
	    }

	    private void ManagePushNotificationMessage(List<string> splitedData, ref byte[] buffer)
	    {
			var notifIndex = splitedData.IndexOf("PUSHNOTIFICATION");

			var bytesBeforePush = 0;
			for (int i = 0; i < notifIndex; i++)
			{
				var data = splitedData[i];

				if (!data.Equals("PUSHNOTIFICATION"))
					bytesBeforePush += data.Length + 1;
				else
					break;
			}

			var dataAfterNotif = splitedData.Skip(notifIndex + 1).ToList();
			var dataBeforeNotif = splitedData.Take(notifIndex).ToList();

			if (dataAfterNotif.Contains("EOCR"))
			{
				var eocrIndex = dataAfterNotif.IndexOf("EOCR");
				
				var notifData = dataAfterNotif.Take(eocrIndex).ToList();
				var dataAfterEocr = dataAfterNotif.Skip(eocrIndex + 1).ToList();

                if (notifData.Count == 2)
                    notifData.Add("");

                Task.Factory.StartNew(AppendNewCustomFileHash(notifData[0], notifData[1], notifData[2]));
				
				var notifSize = 16 + 4 + 2;							//pushLength + eocrLength + 2x':'
				notifData.ForEach(n => notifSize += n.Length + 1);
				var bufferBefore = buffer.Take(bytesBeforePush).ToArray();
				var bufferAfter = buffer.Skip(bytesBeforePush + notifSize).ToArray();
				buffer = bufferBefore.Concat(bufferAfter).ToArray();

                splitedData = Encoding.UTF8.GetString(buffer).Split(':').ToList();

                //splitedData.Clear();
                //if (dataBeforeNotif.Count > 1 && (dataBeforeNotif[0] != "" || dataBeforeNotif[0] != "\0"))
                //	splitedData.AddRange(dataBeforeNotif);
                //if (dataAfterEocr.Count > 1 && (dataAfterEocr[0] != "" || dataBeforeNotif[0] != "\0"))
                //	splitedData.AddRange(dataAfterEocr);
            }
			else
			{
				//read from network till EOCR occurs and then get transmited data !!
				Logger.WriteLine("PushNotification - exception: the message was put in different chunks");
			}
	    }

        private Action AppendNewCustomFileHash(string command, string fileName, string newFileName)
        {
            return () =>
            {
                CustomFileHash customFileHash = null;
                string message;
                string fullLocalPath;
                switch (command)
                {
                    case "CHANGED":
                        message = string.Format("PushNotification: ChangedOnServer - {0}", fileName);
                        fullLocalPath = Helper.GetLocalPath(fileName);
                        customFileHash = new CustomFileHash(FileChangeTypes.ChangedOnServer, fullLocalPath);
                        break;
                    case "MKDIR":
                        message = string.Format("PushNotification: MakedNewDirectory - {0}", fileName);
                        fullLocalPath = Helper.GetLocalPath(fileName);
                        customFileHash = new CustomFileHash(FileChangeTypes.CreatedOnServer, fullLocalPath);
                        break;
                    case "RENAMED":
                        // oldName to newName
                        message = string.Format("PushNotification: RenamedOnServer - {0} to {1}", fileName, newFileName);
                        fullLocalPath = Helper.GetLocalPath(newFileName);
                        var oldFullLocalPath = Helper.GetLocalPath(fileName);
                        customFileHash = new CustomFileHash(FileChangeTypes.RenamedOnServer, fullLocalPath, oldFullLocalPath);
                        break;
                    case "DELETED":
                        message = string.Format("PushNotification: DeletedOnServer - {0}", fileName);
                        fullLocalPath = Helper.GetLocalPath(fileName);
                        customFileHash = new CustomFileHash(FileChangeTypes.DeletedOnServer, fullLocalPath);
                        break;
                    default:
                        message = "Received push notification ISSUE: " + command;
                        break;
                }
                if (customFileHash != null)
                    ChangedFilesList.Add(customFileHash);

                Logger.WriteLine(message);

                return;
            };
        }

	    private void ManageEocrMessage(List<string> splitedData, byte[] buffer)
	    {
            //File.AppendAllText(@"C:\Users\rares\Desktop\TcpCommunicationUTF8.txt", "EOCR MSG: " + Encoding.UTF8.GetString(buffer) + "\n");

            var bytesBeforeEocr = 0;
		    for (var i = 0; i < splitedData.Count(); i++)
		    {
			    var data = splitedData[i];

			    if (!data.Equals("EOCR"))
				    bytesBeforeEocr += data.Length + 1;
			    else break;
		    }

            if (bytesBeforeEocr == 1 && splitedData[0].Equals("") && buffer.Count() == 6)
            {
                CommandResponseBuffer.Complete();
                CommandResponseBuffer = new BufferBlock<byte[]>();
            }
            else
            {
                var dataBeforeEocr = buffer.Take(bytesBeforeEocr).ToArray();

                // Send data before EOCR if there is some
                // We realy need to cut the last colon                              ???
                if (dataBeforeEocr.Length > 0 && dataBeforeEocr[dataBeforeEocr.Length - 1].Equals(byteColon))
                {
                    //dataBeforeEocr = dataBeforeEocr.Take(dataBeforeEocr.Length - 1).ToArray();
                    CommandResponseBuffer.Post(dataBeforeEocr);
                }

                // Wait till the buffer is empty to complete the message            !!!
                while (CommandResponseBuffer.Count > 0)
				    Thread.Sleep(10);
			    CommandResponseBuffer.Complete();
			    CommandResponseBuffer = new BufferBlock<byte[]>();

                // Send remained data if there is some
			    var bytesToEocr = bytesBeforeEocr + 5;
			    if (bytesToEocr < buffer.Count())
			    {
				    var dataAfterEocr = buffer.Skip(bytesToEocr).ToArray();
				    CommandResponseBuffer.Post(dataAfterEocr);
			    }
            }
        }


	    public void SendCommand(byte[] buffer, int offset, int count)
        {
            //File.AppendAllText(@"C:\Users\rares\Desktop\TcpCommunicationUTF8.txt", "COMMAND : " + Encoding.UTF8.GetString(buffer, offset, count) + "\n");
            _networkStream.Write(buffer, offset, count);
	    }
    }
}