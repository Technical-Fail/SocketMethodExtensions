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
