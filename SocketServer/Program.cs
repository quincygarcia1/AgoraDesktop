using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    class Program
    {
        private static byte[] _buffer = new byte[1024];
        private static Socket _serverSoc = 
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private Dictionary<string, Socket> _clients = new Dictionary<string, Socket>();
        private static List<Socket> _identificationBacklog = new List<Socket>();

        static void Main(string[] args)
        {
            Setup();
        }

        private static void Setup()
        {
            Console.WriteLine("setting up...");
            _serverSoc.Bind(new IPEndPoint(IPAddress.Any, 777));
            _serverSoc.Listen(10);
            _serverSoc.BeginAccept(new AsyncCallback(AcceptCallback), null);

        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            Socket soc = _serverSoc.EndAccept(ar);
            _identificationBacklog.Add(soc);
            soc.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), soc);
            _serverSoc.BeginAccept(new AsyncCallback(AcceptCallback), null);

        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            Socket soc = (Socket)ar.AsyncState;
            int dataReceived = soc.EndReceive(ar);

            byte[] tempbuf = new byte[dataReceived];
            Array.Copy(_buffer, tempbuf, dataReceived);

            string text = Encoding.ASCII.GetString(tempbuf);
        }

        private static void SendStrings(string text, Socket clientSoc)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            clientSoc.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), clientSoc);
            clientSoc.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSoc);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            Socket soc = (Socket)ar.AsyncState;
            soc.EndSend(ar);
        }
    }
}
