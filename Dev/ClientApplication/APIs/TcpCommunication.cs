using System;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ClientApplication.APIs
{
    public class TcpCommunication : IDisposable
    {
		public NetworkStream PipeStream { get; set; }


		private readonly TcpClient _tcpClient;
		private readonly NetworkStream _networkStream;
	    private readonly NamedPipeServerStream _pipeServerStream;

	    public TcpCommunication(String hostName, Int32 port)
        {
            try
			{
                _tcpClient = new TcpClient(hostName, port);
                _networkStream = _tcpClient.GetStream();
				_pipeServerStream = new NamedPipeServerStream(Helper.StreamedPipeServerName);

				Task.Factory.StartNew(WaitForConnection());
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

	    private Action WaitForConnection()
	    {
		    return () => _pipeServerStream.WaitForConnection();
	    }

	    public void Dispose()
        {
			_networkStream.Close();
			_tcpClient.Close();
			_pipeServerStream.Close();
        }

	    public void SendCommand(byte[] data, int offset, int size)
	    {
		    _networkStream.Write(data, offset, size);
	    }

		private Action AsyncReading()
		{
			return () =>
			{
				var buffer = new byte[Helper.BufferSize];
				while (true)
				{
					var readBytes = _networkStream.Read(buffer, 0, Helper.BufferSize);

					_pipeServerStream.Write(buffer, 0, readBytes);
				}
			};
		}

//	    public Action ReadResponse()
//	    {
//		    return () =>
//		    {
//			    var buffer = new byte[Helper.BufferSize];
//			    var readBytes = _networkStream.Read(buffer, 0, Helper.BufferSize);
//			    _pipeServerStream.Write(buffer, 0, readBytes);
//		    };
//	    }
    }
}
