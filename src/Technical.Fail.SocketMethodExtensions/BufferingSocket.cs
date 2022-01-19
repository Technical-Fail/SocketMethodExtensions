using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Technical.Fail.SocketMethodExtensions
{
    /// <summary>
    /// Responsible for handling buffering in a seemless way to the consumer.
    /// </summary>
    public class BufferingSocket : IDisposable
    {
        private readonly Socket _socket;
        private Memory<byte> _readBuffer = new Memory<byte>();
        private Memory<byte> _writeBuffer = new Memory<byte>();
        private int _writePosition = 0;
        public BufferingSocket(Socket socket)
        {
            _socket = socket;
            _socket.NoDelay = true;
        }

        public async ValueTask<ReadOnlyMemory<byte>> ReadExactlyAsync(int byteCount, CancellationToken cancellationToken)
        {
            // Get a memory chunk of desired size
            EnsureBufferSize(existingBuffer: ref _readBuffer, requiredSize: byteCount, copyValues: false);
            var memory = _readBuffer.Slice(0, byteCount);

            // Receive exactly this byte count
            int offset = 0;
            int byteCountToReceive = byteCount;

            int bytesReceived = 0;
            while (bytesReceived < byteCountToReceive)
            {
                int readCount = await _socket.ReceiveAsync(buffer: _readBuffer.Slice(offset, byteCountToReceive), socketFlags: SocketFlags.None, cancellationToken: cancellationToken);
                if (readCount == 0)
                    throw new ConnectionClosedException();
                offset += readCount;
                byteCountToReceive -= readCount;
            }

            return memory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="byteCount">The needed byte count to be sent.</param>
        /// <param name="writer">A function that is called with a buffer size of exactly the requested byteCount provided.</param>
        public void Write(int byteCount, Action<Memory<byte>> writer)
        {
            EnsureBufferSize(existingBuffer: ref _writeBuffer, requiredSize: byteCount + _writePosition, copyValues: true);
            var memoryBite = _writeBuffer.Slice(_writePosition, byteCount);
            //Console.WriteLine(GetHashCode() + " writing " + String.Join("|", memoryBite));
            writer(memoryBite);
            _writePosition += byteCount;
        }

        private static void EnsureBufferSize(ref Memory<byte> existingBuffer, int requiredSize, bool copyValues)
        {
            if (requiredSize <= existingBuffer.Length)
                return;

            var newBufferSize = Math.Max(existingBuffer.Length * 2, requiredSize);
            var newBuffer = new Memory<byte>(new byte[newBufferSize]);
            if (copyValues)
            {
                var existingSpan = existingBuffer.Span;
                var newSpan = newBuffer.Span;
                for (var i = 0; i < existingBuffer.Length; i++)
                {
                    newSpan[i] = existingSpan[i];
                }
            }
            existingBuffer = newBuffer;
        }

        public async ValueTask FlushAsync()
        {
            await _socket.SendAsync(_writeBuffer.Slice(0, _writePosition), SocketFlags.None);
            _writePosition = 0;
        }

        public void ShutdownAndClose()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch { }

            try
            {
                _socket.Close();
            }
            catch { }
        }

        public void Dispose()
        {
            ShutdownAndClose();
        }

        public class ConnectionClosedException : Exception { }
    }
}
