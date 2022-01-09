using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Technical.Fail.SocketMethodExtensions.Test
{
    public class BufferingSocketTest
    {
        [Fact]
        public async void SendReceive()
        {
            using (var pair = await SocketTestUtils.ConnectBufferingSocketsAsync())
            {
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 0; memory.Span[1] = 1; memory.Span[2] = 2; });
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 3; memory.Span[1] = 4; memory.Span[2] = 5; });
                await pair.Sender.FlushAsync();

                var receivedTask = pair.Receiver.ReadAsync(byteCount: 6);
                await Task.Delay(TimeSpan.FromSeconds(1));
                Assert.True(receivedTask.IsCompleted);
                var received = await receivedTask;
                Assert.Equal<byte>(new byte[] { 0, 1, 2, 3, 4, 5 }, received.ToArray());
            }
        }

        [Fact]
        public async void SendbigChunksOfDataToTestInternalBufferRegrowing()
        {
            using (var pair = await SocketTestUtils.ConnectBufferingSocketsAsync())
            {
                for (byte i = 0; i < 100; i += 3)
                {
                    pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = i; memory.Span[1] = (byte)(i + 1); memory.Span[2] = (byte)(i + 2); });
                }
                await pair.Sender.FlushAsync();

                {
                    var received = await pair.Receiver.ReadAsync(byteCount: 50);
                    var expectedBytes = new byte[50];
                    for (byte i = 0; i < 50; i++)
                    {
                        expectedBytes[i] = i;
                    }

                    Assert.Equal<byte>(expectedBytes, received.ToArray());
                }

                {
                    var received = await pair.Receiver.ReadAsync(byteCount: 50);
                    var expectedBytes = new byte[50];
                    for (byte i = 0; i < 50; i++)
                    {
                        expectedBytes[i] = (byte)(50 + i);
                    }

                    Assert.Equal<byte>(expectedBytes, received.ToArray());
                }

            }
        }

        [Fact]
        public async void SendReceive_EnsureNotSentUntilFlushIsCalled()
        {
            using (var pair = await SocketTestUtils.ConnectBufferingSocketsAsync())
            {
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 0; memory.Span[1] = 1; memory.Span[2] = 2; });
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 3; memory.Span[1] = 4; memory.Span[2] = 5; });
                var receivedTask = pair.Receiver.ReadAsync(byteCount: 6);
                await Task.Delay(TimeSpan.FromSeconds(1));
                Assert.False(receivedTask.IsCompleted);
                await pair.Sender.FlushAsync();
                var received = await receivedTask;
                Assert.Equal<byte>(new byte[] { 0, 1, 2, 3, 4, 5 }, received.ToArray());
            }
        }

        [Fact]
        public async void SendReceive_EnsureNotReceivedUntilDesiredByteCountAvailable()
        {
            using (var pair = await SocketTestUtils.ConnectBufferingSocketsAsync())
            {
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 0; memory.Span[1] = 1; memory.Span[2] = 2; });
                var receivedTask = pair.Receiver.ReadAsync(byteCount: 6);
                await Task.Delay(TimeSpan.FromSeconds(1));
                Assert.False(receivedTask.IsCompleted);
                pair.Sender.Write(byteCount: 4, writer: memory => { memory.Span[0] = 3; memory.Span[1] = 4; memory.Span[2] = 5; memory.Span[3] = 6; }); //S ending an extra byte to ensure only specified byte count will be received
                await pair.Sender.FlushAsync();
                var received = await receivedTask;
                Assert.Equal<byte>(new byte[] { 0, 1, 2, 3, 4, 5 }, received.ToArray());
            }
        }
    }
}
