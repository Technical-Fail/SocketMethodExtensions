using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Technical.Fail.SocketMethodExtensions
{
    public static class SocketMethodExtensions
    {
        // Used to avoid calling ReceiveExactly multiple times simultaniously

        private static HashSet<Socket> _lockedSockets = new HashSet<Socket>();
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

        public static Task ReceiveExactlyAsync(this Socket socket, ArraySegment<byte> buffer)
        {
            LockSocket(socket);

            int offset = 0;
            int byteCountToReceive = buffer.Count;

            var resultSource = new TaskCompletionSource<bool>(); // Bool is a random value/type as there is no non-generic TaskCompletionSource in dotnet standard 2.1
            Action? beginReceive = null;
            AsyncCallback onReceive = (IAsyncResult ar) =>
            {
                try
                {
                    int readByteCount = socket.EndReceive(ar);
                    if (readByteCount > 0)
                    {
                        // Data received
                        byteCountToReceive -= readByteCount;
                        offset += readByteCount;

                        if (byteCountToReceive > 0)
                        {
                            // Still missing bits    
                            beginReceive();
                        }
                        else
                        {
                            // Done - return
                            resultSource.SetResult(true); // Bool is a random value/type as there is no non-generic TaskCompletionSource in dotnet standard 2.1
                            UnlockSocket(socket);
                        }
                    }
                    else
                    {
                        // Connection closed
                        throw new ConnectionClosedException();
                    }
                }
                catch (Exception ex)
                {
                    resultSource.SetException(ex);
                    UnlockSocket(socket);
                }
            };

            beginReceive = () =>
            {
                socket.BeginReceive(buffers: new List<ArraySegment<byte>>(capacity: 1) { buffer.Slice(offset) }, socketFlags: SocketFlags.None, state: null, callback: onReceive);
            };

            beginReceive();

            return resultSource.Task; // Ensures to unlock socket in the end - no matter how it is finished
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
