using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Technical.Fail.SocketMethodExtensions.Test
{
    public static class SocketTestUtils
    {
        public async static Task<SocketPair> ConnectSocketPairAsync()
        {
            var listener = new TcpListener(localaddr: IPAddress.Parse("127.0.0.1"), port: 0);
            listener.Start();

            var serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), ((IPEndPoint)listener.LocalEndpoint).Port);
            Socket clientSocket = new Socket(serverEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Connect client and server
            listener.Start();
            Task connectTask = clientSocket.ConnectAsync(serverEndpoint);
            Task<Socket> acceptTask = listener.AcceptSocketAsync();
            await connectTask;
            Socket serverSideSocket = await acceptTask;
            listener.Stop();
            return new SocketPair(socket1: serverSideSocket, socket2: clientSocket);
        }

        public static void AssertEqual(IEnumerable<byte> expected, IEnumerable<byte> actual)
        {
            Assert.Equal(string.Join(",", expected), string.Join(",", actual));
        }
    }

    public class SocketPair : IDisposable
    {
        public Socket Socket1 { get; }
        public Socket Socket2 { get; }
        public SocketPair(Socket socket1, Socket socket2)
        {
            Socket1 = socket1 ?? throw new ArgumentNullException(nameof(socket1));
            Socket2 = socket2 ?? throw new ArgumentNullException(nameof(socket2));
        }

        public void Dispose()
        {
            try
            {
                Socket1?.Close();
            }
            catch { }
            try
            {
                Socket2?.Close();
            }
            catch { }
        }
    }
}


