using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;

namespace ClientApplication.APIs
{
    public class TcpCommunication : IDisposable
    {
	    private readonly NetworkStream _networkStream;
		public BufferBlock<byte[]> CommandResponseBuffer;

	    public TcpCommunication(String hostName, Int32 port)
	    {
		    try
            {
                var tcpClient = new TcpClient(hostName, port);
                _networkStream = tcpClient.GetStream();

				CommandResponseBuffer = new BufferBlock<byte[]>();
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

						var readData = Encoding.Default.GetString(buffer, 0, readBytes);
						var splitedData = readData.Split(':');
					    var eocr = splitedData.Any(s => s.Equals("EOCR"));
					    if (eocr)
					    {
							ManageEocrMessage(splitedData, buffer, readBytes);
					    }
					    else
					    {
						    CommandResponseBuffer.Post(buffer);
					    }
				    }
				    catch (Exception ex)
				    {
						MessageBox.Show(ex.Message, @"AsyncReading", MessageBoxButtons.OK, MessageBoxIcon.Error);
				    }
			    }
		    };
	    }

	    private void ManageEocrMessage(IEnumerable<string> splitedData, ICollection<byte> buffer, int readBytes)
	    {
		    var bytesBeforeEocr = 0;
		    foreach (var data in splitedData)
		    {
			    if (!data.Equals("EOCR"))
				    bytesBeforeEocr += data.Length + 1;
			    else
				    break;
		    }

		    if (bytesBeforeEocr > 0)
		    {
			    var dataBeforeEocr = buffer.Take(bytesBeforeEocr).ToArray();
			    CommandResponseBuffer.Post(dataBeforeEocr);
			    CommandResponseBuffer.Complete();

			    CommandResponseBuffer = new BufferBlock<byte[]>();
			    var bytesWithEocr = bytesBeforeEocr + 5;
				if (bytesWithEocr < readBytes)
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
	    }

	    public void SendCommand(byte[] buffer, int offset, int count)
	    {
		    _networkStream.Write(buffer, offset, count);
	    }
    }
}