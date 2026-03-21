using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebAPI.Judging;

public static class BridgePacketCodec
{
    public static async Task<Dictionary<string , JsonElement>?> ReadPacketAsync(Stream stream , CancellationToken cancellationToken)
    {
        var sizeBuffer = new byte[4];
        var headerRead = await ReadExactAsync(stream , sizeBuffer , cancellationToken);
        if ( headerRead == 0 )
            return null;

        if ( headerRead != 4 )
            throw new IOException("Invalid packet header length.");

        var size = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(sizeBuffer , 0));
        if ( size <= 0 )
            throw new IOException("Invalid packet size.");

        var payloadBuffer = new byte[size];
        var payloadRead = await ReadExactAsync(stream , payloadBuffer , cancellationToken);
        if ( payloadRead != size )
            throw new IOException("Incomplete packet payload.");

        using var payloadStream = new MemoryStream(payloadBuffer);
        using var zlib = new ZLibStream(payloadStream , CompressionMode.Decompress);
        using var output = new MemoryStream();
        await zlib.CopyToAsync(output , cancellationToken);

        var json = Encoding.UTF8.GetString(output.ToArray());
        return JsonSerializer.Deserialize<Dictionary<string , JsonElement>>(json);
    }

    public static async Task WritePacketAsync(Stream stream , object packet , CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(packet);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        using var compressedOutput = new MemoryStream();
        using ( var zlib = new ZLibStream(compressedOutput , CompressionLevel.SmallestSize , true) )
        {
            await zlib.WriteAsync(jsonBytes , cancellationToken);
        }

        var compressedBytes = compressedOutput.ToArray();
        var sizeBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(compressedBytes.Length));

        await stream.WriteAsync(sizeBytes , cancellationToken);
        await stream.WriteAsync(compressedBytes , cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<int> ReadExactAsync(Stream stream , byte[] buffer , CancellationToken cancellationToken)
    {
        var offset = 0;
        while ( offset < buffer.Length )
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset , buffer.Length - offset) , cancellationToken);
            if ( read == 0 )
                return offset;

            offset += read;
        }

        return offset;
    }
}