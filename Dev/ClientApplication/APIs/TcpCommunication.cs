using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;
using ClientApplication.Models;

namespace ClientApplication.APIs
{
    public class TcpCommunication : IDisposable
	{
		private TcpClient _tcpClient;
		private readonly NetworkStream _networkStream;
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
			// ReSharper disable once FunctionNeverReturns
			return () =>
		    {
			    while (true)
			    {
				    try
				    {
					    var buffer = new byte[Helper.BufferSize];
					    var readBytes = _networkStream.Read(buffer, 0, Helper.BufferSize);
					    buffer = buffer.Take(readBytes).ToArray();

					    var readData = Encoding.UTF8.GetString(buffer, 0, readBytes);
					    var splitedData = readData.Split(':').ToList();

					    var pushNotification = splitedData.Any(s => s.Equals("PUSHNOTIFICATION"));
					    if (pushNotification)
					    {
						    ManagePushNotificationMessage(splitedData, ref buffer);
					    }

					    var eocr = splitedData.Any(s => s.Equals("EOCR"));
					    if (eocr)
					    {
						    ManageEocrMessage(splitedData, buffer);
					    }
					    else if (buffer.Length > 0)
					    {
						    CommandResponseBuffer.Post(buffer);
					    }
					    else
						{
							CommandResponseBuffer.Complete();
							CommandResponseBuffer = new BufferBlock<byte[]>();
							if (_customFileHash != null)
							{
								ChangedFilesList.Add(_customFileHash);
								_customFileHash = null;
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

		CustomFileHash _customFileHash;
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

				var command = notifData[0];

				_customFileHash = null;
				string message;
				string fullLocalPath;
				switch (command)
				{
					case "CHANGED":
						message = String.Format("PushNotification: ChangedOnServer - {0}", notifData[1]);
						fullLocalPath = Helper.GetLocalPath(notifData[1]);
						_customFileHash = new CustomFileHash(FileChangeTypes.ChangedOnServer, fullLocalPath);
						break;
					case "RENAMED":
						message = String.Format("PushNotification: RenamedOnServer - {0} to {1}", notifData[2], notifData[1]);
						fullLocalPath = Helper.GetLocalPath(notifData[2]);
						var oldFullLocalPath = Helper.GetLocalPath(notifData[1]);
						_customFileHash = new CustomFileHash(FileChangeTypes.RenamedOnServer, fullLocalPath, oldFullLocalPath);
						break;
					case "DELETED":
						message = String.Format("PushNotification: DeletedOnServer - {0}", notifData[1]);
						fullLocalPath = Helper.GetLocalPath(notifData[1]);
						_customFileHash = new CustomFileHash(FileChangeTypes.DeletedOnServer, fullLocalPath);
						break;
					default:
						message = "Received push notification ISSUE: " + command;
						break;
				}

				Logger.WriteLine(message);
				
				var notifSize = 16 + 4 + 2;							//pushLength + eocrLength + 2x':'
				notifData.ForEach(n => notifSize += n.Length + 1);
				var bufferBefore = buffer.Take(bytesBeforePush).ToArray();
				var bufferAfter = buffer.Skip(bytesBeforePush + notifSize).ToArray();
				buffer = bufferBefore.Concat(bufferAfter).ToArray();

				splitedData.Clear();

				if (dataBeforeNotif.Count > 1 && dataBeforeNotif[0] != "")
					splitedData.AddRange(dataBeforeNotif);

				if (dataAfterEocr.Count > 1 && dataAfterEocr[0] != "")
					splitedData.AddRange(dataAfterEocr);

			}
			else
			{
				//read from network till EOCR occurs and then get transmited data !!
				Logger.WriteLine("PushNotification - exception: the message was put in different chunks");
			}
	    }

	    private void ManageEocrMessage(IReadOnlyList<string> splitedData, ICollection<byte> buffer)
	    {
		    var bytesBeforeEocr = 0;
		    var bytesWithEocr = 0;
		    for (var i = 0; i < splitedData.Count(); i++)
		    {
			    var data = splitedData[i];

			    if (!data.Equals("EOCR"))
				    bytesBeforeEocr += data.Length + 1;
			    else
			    {
				    break;
			    }
		    }

			if (bytesBeforeEocr > 0 && splitedData[0] != "")
		    {
			    var dataBeforeEocr = buffer.Take(bytesBeforeEocr).ToArray();

			    if (dataBeforeEocr[dataBeforeEocr.Length - 1] == Encoding.UTF8.GetBytes(":")[0])
				    dataBeforeEocr = dataBeforeEocr.Take(dataBeforeEocr.Length - 1).ToArray();

			    CommandResponseBuffer.Post(dataBeforeEocr);

			    while (CommandResponseBuffer.Count > 0)
				    Thread.Sleep(10);
			    CommandResponseBuffer.Complete();
			    CommandResponseBuffer = new BufferBlock<byte[]>();

			    bytesWithEocr += bytesBeforeEocr + 5;
			    if (bytesWithEocr < buffer.Count)
			    {
				    var dataAfterEocr = buffer.Skip(bytesWithEocr).ToArray();
				    CommandResponseBuffer.Post(dataAfterEocr);
			    }
		    }
		    else
		    {
			    CommandResponseBuffer.Complete();
			    CommandResponseBuffer = new BufferBlock<byte[]>();
		    }

			//// In case there is a transmission on going ?!!?!??!!?!?!
		    //if (_customFileHash != null)
		    //{
			//    ChangedFilesList.Add(_customFileHash);
			//    _customFileHash = null;
		    //}
	    }

	    public void SendCommand(byte[] buffer, int offset, int count)
	    {
		    _networkStream.Write(buffer, offset, count);
	    }
    }
}