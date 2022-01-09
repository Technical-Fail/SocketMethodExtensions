using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Technical.Fail.SocketMethodExtensions
{
    public interface IBufferingSocket
    {
        ValueTask<ReadOnlyMemory<byte>> ReadExactlyAsync(int byteCount);
        ValueTask FlushAsync();
        void Write(int byteCount, Action<Memory<byte>> writer);
    }

    public class BufferingSocket : IBufferingSocket, IDisposable
    {
        private readonly Socket _socket;
        private Memory<byte> _readBuffer = new Memory<byte>();
        private Memory<byte> _writeBuffer = new Memory<byte>();
        private int _writePosition = 0;
        public BufferingSocket(Socket socket)
        {
            _socket = socket;
        }

        public async ValueTask<ReadOnlyMemory<byte>> ReadExactlyAsync(int byteCount)
        {
            _readBuffer = EnsureBufferSize(existingBuffer: _readBuffer, minimumSize: byteCount, copyValues: false);

            var memory = _readBuffer.Slice(0, byteCount);
            await _socket.ReceiveExactlyAsync(memory);
            return memory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="byteCount">The needed byte count to be sent.</param>
        /// <param name="writer">A function that is called with a buffer size of exactly the requested byteCount provided.</param>
        public void Write(int byteCount, Action<Memory<byte>> writer)
        {
            _writeBuffer = EnsureBufferSize(existingBuffer: _writeBuffer, minimumSize: byteCount + _writePosition, copyValues: true);
            var memoryBite = _writeBuffer.Slice(_writePosition, byteCount);
            writer(memoryBite);
            _writePosition += byteCount;
        }

        private static Memory<byte> EnsureBufferSize(Memory<byte> existingBuffer, int minimumSize, bool copyValues)
        {
            if (minimumSize <= existingBuffer.Length)
                return existingBuffer;
            var newBufferSize = Math.Max(existingBuffer.Length * 2, minimumSize);
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
            return newBuffer;
        }

        public async ValueTask FlushAsync()
        {
            await _socket.SendAsync(_writeBuffer, SocketFlags.None);
            _writePosition = 0;
        }

        public void Dispose()
        {
            _socket.Dispose();
        }

        internal void Disconnect()
        {
            _socket.Disconnect(reuseSocket: false);
        }
    }
}
