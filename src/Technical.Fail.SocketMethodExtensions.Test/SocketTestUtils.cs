using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Technical.Fail.SocketMethodExtensions.Test
{
    internal static class SocketTestUtils
    {
        public static async Task<SocketPair> ConnectSocketsAsync()
        {
            var listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Start();
            var client = new TcpClient();
            await client.ConnectAsync((IPEndPoint)listener.LocalEndpoint);
            var serverSideSocket = await listener.AcceptSocketAsync();
            listener.Stop();
            return new SocketPair(socket1: client.Client, socket2: serverSideSocket);
        }
        public static async Task<BufferingSocketPair> ConnectBufferingSocketsAsync()
        {
            return new BufferingSocketPair(await ConnectSocketsAsync());
        }

        public static void AssertEqual(IEnumerable<byte> expected, IEnumerable<byte> actual)
        {
            Assert.Equal(string.Join(",", expected), string.Join(",", actual));
        }
    }

    internal class SocketPair : IDisposable
    {
        public Socket Socket1 { get; set; }
        public Socket Socket2 { get; set; }

        public SocketPair(Socket socket1, Socket socket2)
        {
            Socket1 = socket1;
            Socket2 = socket2;
        }

        public void Dispose()
        {
            Socket1.Dispose();
            Socket2.Dispose();
        }
    }

    internal class BufferingSocketPair : IDisposable
    {
        public BufferingSocket S1 { get; set; }
        public BufferingSocket S2 { get; set; }

        // For better readability in tests, these names can be used, too:
        public BufferingSocket Sender => S1;
        public BufferingSocket Receiver => S2;
        public BufferingSocketPair(SocketPair socketPair)
        {
            S1 = new BufferingSocket(socketPair.Socket1);
            S2 = new BufferingSocket(socketPair.Socket2);
        }

        public void Dispose()
        {
            S1.Dispose();
            S2.Dispose();
        }
    }
}


