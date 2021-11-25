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
