# SocketMethodExtensions

This repo is built as Technical.Fail.SocketMethodExtensions on nuget.org

## Usage (async)

```cs
Socket mySocket = ...;
var buffer = new byte[100];
ArraySegment<byte> segmentBuffer = buffer;
// Get exactly 10 bytes and store it from position 0 in the buffer:
await mySocket.ReceiveExactlyAsync(buffer: segmentBuffer.Slice(0, 10));
```

## Usage (Sync blocking)

```cs
Socket mySocket = ...;
var buffer = new byte[100];
ArraySegment<byte> segmentBuffer = buffer;
// Get exactly 10 bytes and store it from position 0 in the buffer:
pair.Socket2.ReceiveExactlyBlocking(buffer: segmentBuffer.Slice(0, 10));
```

## Notes

Any network protocol will ultimately use one of two strategies for delimiting data:

### Specifying size in message (Supported by this repo)

Use a fixed sized message or specify the length of the next message with the first bytes in the next message. This repo is built for use with this  type of protocols.

### Specifying a delimiter in the end of a message (Not yet supported)

Using a delimiter to specify where one message stop and another message begin. In the future I plan on adding method extensions to wait for data until a certain delimiter specified as a fixed range of bytes has been detected.


# BufferingSocket

This class handles buffering and allows for onliners for both reading and writing.

## ReadExactlyAsync

```cs
Socket socket = ...
var bufferingSocket = new BufferingSocket(socket);
Span<byte> bytes = bufferingSocket.ReadExactlyAsync(byteCount: 6); // Task will complete when exactly 6 bytes are received
```

## Writing

The write method allows for multiple write calls before a final flush is done, spasring the network connection and prevents fragmented ip packages to be sent until the consumer wants this to happen.

```cs
Socket socket = ...
var bufferingSocket = new BufferingSocket(socket);
socket.Write(byteCount: 3, writer: memory => {
	// Memory is exactly 3 bytes long here
	memory.Span[0] = 0;
	memory.Span[1] = 1;
	memory.Span[2] = 2;
});

socket.Write(byteCount: 1, writer: memory => {
	// Memory is exactly 1 bytes long here
	memory.Span[0] = 10;
});

// Nothing is actually sent until FlushAsync has been called.
await socket.FlushAsync();
```
