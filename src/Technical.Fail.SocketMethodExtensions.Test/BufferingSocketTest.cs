using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Technical.Fail.SocketMethodExtensions.Test
{
    public class BufferingSocketTest
    {
        [Fact]
        public async void Send_PreviousBug_FlushingMoreBytesThanWasWritten_Async()
        {
            using (var pair = await SocketTestUtils.ConnectBufferingSocketsAsync())
            {
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 0; memory.Span[1] = 1; memory.Span[2] = 2; });
                pair.Sender.Write(byteCount: 1, writer: memory => { memory.Span[0] = 3; });
                await pair.Sender.FlushAsync();

                var b0 = await pair.Receiver.ReadExactlyAsync(byteCount: 1, cancellationToken: CancellationToken.None);
                var b1 = await pair.Receiver.ReadExactlyAsync(byteCount: 1, cancellationToken: CancellationToken.None);
                var b2 = await pair.Receiver.ReadExactlyAsync(byteCount: 1, cancellationToken: CancellationToken.None);
                var b3 = await pair.Receiver.ReadExactlyAsync(byteCount: 1, cancellationToken: CancellationToken.None);

                // Ensure no more bytes sent
                var cancelTokenSource = new CancellationTokenSource();
                cancelTokenSource.CancelAfter(TimeSpan.FromSeconds(1));
                var ex = await Assert.ThrowsAsync<OperationCanceledException>(async () => await pair.Receiver.ReadExactlyAsync(byteCount: 1, cancellationToken: cancelTokenSource.Token));
            }
        }

        [Fact]
        public async void Send_ReadExactlyAsync()
        {
            using (var pair = await SocketTestUtils.ConnectBufferingSocketsAsync())
            {
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 0; memory.Span[1] = 1; memory.Span[2] = 2; });
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 3; memory.Span[1] = 4; memory.Span[2] = 5; });
                await pair.Sender.FlushAsync();

                var receivedTask = pair.Receiver.ReadExactlyAsync(byteCount: 6, cancellationToken: new CancellationTokenSource().Token);
                await Task.Delay(TimeSpan.FromSeconds(1));
                Assert.True(receivedTask.IsCompleted);
                var received = await receivedTask;
                Assert.Equal<byte>(new byte[] { 0, 1, 2, 3, 4, 5 }, received.ToArray());
            }
        }

        [Fact]
        public async void Send_ReadExactlyAsync_BigChunksOfDataToTestInternalBufferRegrowing()
        {
            using (var pair = await SocketTestUtils.ConnectBufferingSocketsAsync())
            {
                for (byte i = 0; i < 100; i += 3)
                {
                    pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = i; memory.Span[1] = (byte)(i + 1); memory.Span[2] = (byte)(i + 2); });
                }
                await pair.Sender.FlushAsync();

                {
                    var received = await pair.Receiver.ReadExactlyAsync(byteCount: 50, new CancellationTokenSource().Token);
                    var expectedBytes = new byte[50];
                    for (byte i = 0; i < 50; i++)
                    {
                        expectedBytes[i] = i;
                    }

                    Assert.Equal<byte>(expectedBytes, received.ToArray());
                }

                {
                    var received = await pair.Receiver.ReadExactlyAsync(byteCount: 50, new CancellationTokenSource().Token);
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
        public async void Send_ReadExactlyAsync_EnsureNotSentUntilFlushIsCalled()
        {
            using (var pair = await SocketTestUtils.ConnectBufferingSocketsAsync())
            {
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 0; memory.Span[1] = 1; memory.Span[2] = 2; });
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 3; memory.Span[1] = 4; memory.Span[2] = 5; });
                var receivedTask = pair.Receiver.ReadExactlyAsync(byteCount: 6, new CancellationTokenSource().Token);
                await Task.Delay(TimeSpan.FromSeconds(1));
                Assert.False(receivedTask.IsCompleted);
                await pair.Sender.FlushAsync();
                var received = await receivedTask;
                Assert.Equal<byte>(new byte[] { 0, 1, 2, 3, 4, 5 }, received.ToArray());
            }
        }

        [Fact]
        public async void Send_ReadExactlyAsync_EnsureNotReceivedUntilDesiredByteCountAvailable()
        {
            using (var pair = await SocketTestUtils.ConnectBufferingSocketsAsync())
            {
                pair.Sender.Write(byteCount: 3, writer: memory => { memory.Span[0] = 0; memory.Span[1] = 1; memory.Span[2] = 2; });
                var receivedTask = pair.Receiver.ReadExactlyAsync(byteCount: 6, new CancellationTokenSource().Token);
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
