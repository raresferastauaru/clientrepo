using ClientApplicationWpf.Helpers;
using ClientApplicationWpf.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;

namespace ClientApplicationWpf.APIs
{
    public class TcpCommunication : IDisposable
    {
        #region Properties
        private byte byteColon = Encoding.UTF8.GetBytes(":")[0];
        private byte byteEmpty = Encoding.UTF8.GetBytes("\0")[0];

        private byte[] bytesEocr = Encoding.UTF8.GetBytes("EOCR:").ToArray();
        private byte[] bytesPushNotification = Encoding.UTF8.GetBytes("PUSHNOTIFICATION:").ToArray(); //:PushNotification: ????


        private TcpClient _tcpClient;
        private NetworkStream _networkStream;

        public BufferBlock<byte[]> CommandResponseBuffer { get; private set; }
        public ThreadSafeList<CustomFileHash> ChangedFilesList { get; private set; }
        #endregion Properties

        #region ConstructorsDestructors
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
        #endregion ConstructorsDestructors

        #region ReceivedMessages
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
                        var cachedData = readData;
                        var splitedData = readData.Split(':').ToList();
                        //File.AppendAllText(@"C:\Users\rares\Desktop\TcpCommunicationUTF8.txt", "RESPONSE: " + readData + "\n");

                        var pushNotificationMessage = splitedData.Any(s => s.Equals("PUSHNOTIFICATION"));
                        if (pushNotificationMessage)
                        {
                            // If it contains only the Push then the buffer should be EMPTY after
                            // 2 cases: "PUSHNOTIFICATION:data4:data5:data6:EOCR:"
                            //          ":PUSHNOTIFICATION:data4:data5:data6:EOCR:"
                            // resulting an empty buffer.

                            ManagePushNotificationMessage(splitedData, ref buffer);
                        }

                        var eocrMessage = splitedData.Any(s => s.Equals("EOCR"));
                        if (eocrMessage)
                        {
                            ManageEocrMessage(splitedData, buffer);
                        }
                        else
                        {
                            // DATA message
                            if (buffer.Length > 0)
                            {
                                //if (buffer.Length == 1 && buffer[0].Equals(byteEmpty))
                                //{
                                //    Logger.WriteLine("~~~AsyncReading: buffer.Length == 1 && emptyByte. WHY ?!\n" + 
                                //                     "~~~\tCachedData: " + cachedData);
                                //}
                                //else
                                //{ 
                                CommandResponseBuffer.Post(buffer);
                                //}
                            }
                            else
                            {
                                Logger.WriteLine("~~~AsyncReading: buffer.Length = 0. WHY ?!\n" +
                                                 "~~~\tCachedData: " + cachedData);
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Thread is async and it tries to use a disposed object.
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
                        MessageBox.Show(str, @"AsyncReading - Exception - " + ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };
        }

        private void ManageEocrMessage(List<string> splitedData, byte[] buffer)
        {
            var bytesBeforeEocr = ByteArrayHelper.IndexOfSequence(buffer, bytesEocr);

            // is there a message like: ::EOCR: ???
            if ((bytesBeforeEocr == 1 && splitedData[0].Equals("") && buffer.Count() == 6)
               || (splitedData[0].Equals("EOCR") && buffer.Count() == 5))
            {
                //IF message is :EOCR: OR EOCR:
                CommandResponseBuffer.Complete();
                CommandResponseBuffer = new BufferBlock<byte[]>();
            }
            else
            {
                var dataBeforeEocr = buffer.Take(bytesBeforeEocr).ToArray();

                // Send data before EOCR if there is some
                if (dataBeforeEocr.Length > 0 && dataBeforeEocr[dataBeforeEocr.Length - 1].Equals(byteColon))
                {
                    // Cuts the ":" before EOCR and sends data.
                    dataBeforeEocr = dataBeforeEocr.Take(dataBeforeEocr.Length - 1).ToArray();
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
                    Logger.WriteLine("~~~AsyncReading(EOCR - RemainedData) : " + Encoding.UTF8.GetString(buffer));
                    var dataAfterEocr = buffer.Skip(bytesToEocr).ToArray();
                    CommandResponseBuffer.Post(dataAfterEocr);
                }
                // This is not necessary: else if (bytesToEocr == buffer.Count()) !!! I THINK !!!
            }
        }

        private void ManagePushNotificationMessage(List<string> splitedData, ref byte[] buffer)
        {
            //Logger.WriteLine("~~~PushNotifiation - BUFFER BEFORE -> " + Encoding.UTF8.GetString(buffer));

            var indexOfPushNotif = ByteArrayHelper.IndexOfSequence(buffer, bytesPushNotification);

            var dataBeforeNotif = buffer.Take(indexOfPushNotif).ToArray();
            var dataAfterNotifWithData = buffer.Skip(indexOfPushNotif + bytesPushNotification.Length).ToArray();

            // If PushNotification Contains EOCR
            if (ByteArrayHelper.IndexOfSequence(buffer, bytesEocr) >= 0)
            {
                var eocrIndex = ByteArrayHelper.IndexOfSequence(buffer, bytesEocr);

                var notifDataBytes = dataAfterNotifWithData.Take(eocrIndex).ToArray();
                var notifData = Encoding.UTF8.GetString(notifDataBytes).Split(':').ToList();
                if (string.IsNullOrEmpty(notifData[0]))
                    notifData.Skip(1);

                var dataAfterNotif = dataAfterNotifWithData.Skip(eocrIndex + 5).ToArray();

                buffer = dataBeforeNotif.Concat(dataAfterNotif).ToArray();

                // This will exclude buffer=":" (PushNotifs that start with ":") case.
                if (buffer.Length == 1 && buffer[0] == byteColon)
                {
                    buffer = new byte[0];
                }

                if (buffer.Length > 2 && buffer[indexOfPushNotif - 1] == byteColon && buffer[indexOfPushNotif - 2] == byteColon)
                {
                    buffer = buffer.Take(buffer.Length - 1).ToArray();
                }

                if (buffer.Length > 0)
                {
                    splitedData = Encoding.UTF8.GetString(buffer).Split(':').ToList();
                }
                else
                {
                    CommandResponseBuffer.Complete();
                    CommandResponseBuffer = new BufferBlock<byte[]>();
                    splitedData = new List<string>();
                }

                //Logger.WriteLine("~~~PushNotifiation - BUFFER AFTER -> " + Encoding.UTF8.GetString(buffer));
                Task.Factory.StartNew(AppendNewCustomFileHash(notifData[0], notifData[1], notifData[2]));
            }
            else
            {
                //read from network till EOCR occurs and then get transmited data !!
                Logger.WriteLine("PushNotification - exception: the message was put in different chunks !! But HOW ?!");
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
                        message = string.Format("Received push notification ISSUE:\n\tCommand: {0}\n\tFileName: {1}\n\tNewFileName: {2}", command, fileName, newFileName);
                        break;
                }
                if (customFileHash != null)
                    ChangedFilesList.Add(customFileHash);

                Logger.WriteLine(message);

                return;
            };
        }
        #endregion ReceivedMessages

        #region SentMessages
        public void SendCommand(byte[] buffer, int offset, int count)
        {
            //File.AppendAllText(@"C:\Users\rares\Desktop\TcpCommunicationUTF8.txt", "COMMAND : " + Encoding.UTF8.GetString(buffer, offset, count) + "\n");
            _networkStream.Write(buffer, offset, count);
        }
        #endregion SentMessages
    }
}
