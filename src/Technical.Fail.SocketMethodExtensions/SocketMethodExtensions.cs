using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Technical.Fail.SocketMethodExtensions
{
    public static class SocketMethodExtensions
    {
        // Used to avoid calling ReceiveExactly multiple times simultaniously

        private static readonly HashSet<Socket> _lockedSockets = new HashSet<Socket>();
        private static void LockSocket(Socket socket)
        {
            lock (_lockedSockets)
            {
                if (_lockedSockets.Contains(socket))
                    throw new AlreadyListeningException("Already receiving on socket!");
                _lockedSockets.Add(socket);
            }
        }

        private static void UnlockSocket(Socket socket)
        {
            lock (_lockedSockets)
            {
                if (_lockedSockets.Contains(socket))
                    _lockedSockets.Remove(socket);
            }
        }

        public static void ReceiveExactlyBlocking(this Socket socket, ArraySegment<byte> buffer)
        {
            LockSocket(socket);
            try
            {
                int offset = 0;
                int byteCountToReceive = buffer.Count;

                int bytesReceived = 0;
                while (bytesReceived < byteCountToReceive)
                {
                    int readCount = socket.Receive(buffer: buffer.AsSpan().Slice(offset, byteCountToReceive), socketFlags: SocketFlags.None);
                    if (readCount == 0)
                        throw new ConnectionClosedException();
                    offset += readCount;
                    byteCountToReceive -= readCount;
                }
            }
            finally
            {
                UnlockSocket(socket);
            }
        }

        public static async Task ReceiveExactlyAsync(this Socket socket, ArraySegment<byte> buffer)
        {
            LockSocket(socket);
            try
            {
                int offset = 0;
                int byteCountToReceive = buffer.Count;

                int bytesReceived = 0;
                while (bytesReceived < byteCountToReceive)
                {
                    int readCount = await socket.ReceiveAsync(buffer: buffer.AsMemory().Slice(offset, byteCountToReceive), socketFlags: SocketFlags.None);
                    if (readCount == 0)
                        throw new ConnectionClosedException();
                    offset += readCount;
                    byteCountToReceive -= readCount;
                }
            }
            finally
            {
                UnlockSocket(socket);
            }
        }
    }

    public class ConnectionClosedException : Exception { }

    public class AlreadyListeningException : Exception
    {
        public AlreadyListeningException(string message) : base(message)
        {
        }
    }

}
