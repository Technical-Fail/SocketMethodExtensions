using System;
using System.Threading.Tasks;
using Xunit;
using System.Threading;

namespace Technical.Fail.SocketMethodExtensions.Test
{
    public class SocketMethodExtensions_ReceiveExactlyBlocking_Test
    {
        [Fact]
        public async void ReceiveExactlyBlocking_SingleChunk_SendAndReceiveSameCount_Test()
        {
            using (var pair = await SocketTestUtils.ConnectSocketsAsync())
            {
                // Send 10 bytes
                pair.Socket1.Send(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

                var receiveBuffer = new byte[15];
                ArraySegment<byte> segmentBuffer = receiveBuffer;

                pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(0, 10));

                SocketTestUtils.AssertEqual(expected: new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0 }, actual: receiveBuffer);
            }
        }

        [Fact]
        public async void ReceiveExactlyBlocking_SendInSmallChunks_Test()
        {
            using (var pair = await SocketTestUtils.ConnectSocketsAsync())
            {
                // Send 10 bytes
                pair.Socket1.Send(new byte[] { 0, 1, 2, 3 });
                pair.Socket1.Send(new byte[] { 4, 5, 6, 7 });

                // Wait 2 seconds before sending more data
                var task = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => pair.Socket1.Send(new byte[] { 8, 9, 3, 3, 3, 3, 3, 3, 3 }));

                // Start receiving right away to ensure waiting for the last 2 bytes to be sent to have a total of 10 bytes to receive
                var receiveBuffer = new byte[15];
                ArraySegment<byte> segmentBuffer = receiveBuffer;

                pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(0, 10));

                SocketTestUtils.AssertEqual(expected: new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0 }, actual: receiveBuffer);
            }
        }

        [Fact]
        public async void ReceiveExactlyBlocking_SendInSmallChunksWithOffset_Test()
        {
            using (var pair = await SocketTestUtils.ConnectSocketsAsync())
            {
                // Send 10 bytes
                pair.Socket1.Send(new byte[] { 0, 1, 2, 3 });
                pair.Socket1.Send(new byte[] { 4, 5, 6, 7 });

                // Wait 2 seconds before sending more data
                var task = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => pair.Socket1.Send(new byte[] { 8, 9, 1, 2, 3, 3, 3, 3, 3 }));

                // Start receiving right away to ensure waiting for the last 2 bytes to be sent to have a total of 10 bytes to receive
                var receiveBuffer = new byte[15];
                ArraySegment<byte> segmentBuffer = receiveBuffer;

                pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(3, 10));

                SocketTestUtils.AssertEqual(expected: new byte[] { 0, 0, 0, // <= offset by 3
                                                   0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0 }, actual: receiveBuffer);
            }
        }

        [Fact]
        public async void ReceiveExactlyBlocking_ReceiveInSmallerChunksThanSending_Test()
        {
            using (var pair = await SocketTestUtils.ConnectSocketsAsync())
            {
                // Send 17 bytes
                pair.Socket1.Send(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 8, 7, 6, 5, 4, 3, 2, 1, 5, 5, 5, 5 });

                // Start receiving right away to ensure waiting for the last 2 bytes to be sent to have a total of 10 bytes to receive
                var receiveBuffer = new byte[20];
                ArraySegment<byte> segmentBuffer = receiveBuffer;

                pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(0, 5));
                pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(5, 3));
                pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(8, 9));

                SocketTestUtils.AssertEqual(expected: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0, 0 }, actual: receiveBuffer);
            }
        }

        [Fact]
        public async void ReceiveExactlyBlocking_MultipleReceiveCalls_AlreadyCallingWithBlocking_ExpectException_Test()
        {
            using (var pair = await SocketTestUtils.ConnectSocketsAsync())
            {
                var receiveBuffer = new byte[20];
                ArraySegment<byte> segmentBuffer = receiveBuffer;

                var thread = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(0, 5));
                    }
                    catch { }
                }));
                thread.Start();
                Thread.Sleep(TimeSpan.FromSeconds(2));
                Assert.Throws<AlreadyListeningException>(() => pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(0, 5)));
            }
        }

        [Fact]
        public async void ReceiveExactlyBlocking_MultipleReceiveCalls_AlreadyCallingWithAsync_ExpectException_Test()
        {
            using (var pair = await SocketTestUtils.ConnectSocketsAsync())
            {
                var byteBuffer = new byte[20];
                ArraySegment<byte> segmentBuffer = byteBuffer;
                var task = pair.Socket2.ReceiveExactlyAsync(buffer: segmentBuffer.Slice(0, 5));
                Assert.Throws<AlreadyListeningException>(() => pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(0, 5)));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public async void ConnectionClosingWhileReceiving_Test(uint closeDelaySeconds)
        {
            using (var pair = await SocketTestUtils.ConnectSocketsAsync())
            {
                // Send 10 bytes
                pair.Socket1.Send(new byte[] { 0, 1, 2, 3 });
                pair.Socket1.Send(new byte[] { 4, 5, 6, 7 });

                if (closeDelaySeconds == 0)
                {
                    pair.Socket1.Close();
                }
                else
                {
                    // Wait 2 seconds before sending more data
                    var task = Task.Delay(TimeSpan.FromSeconds(closeDelaySeconds)).ContinueWith(t => pair.Socket1.Close());
                }

                var byteBuffer = new byte[15];
                ArraySegment<byte> segmentBuffer = byteBuffer;
                Assert.Throws<ConnectionClosedException>(() => pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(0, 10)));
            }
        }


    }
}