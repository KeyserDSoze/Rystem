using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class AzureOpenAIVisionLiveGateTests : PlayFrameworkTestBase
{
    public AzureOpenAIVisionLiveGateTests()
        : base(
            useRealAzureOpenAI: true,
            deploymentEnvironmentVariable: "AZURE_OPENAI_VISION_DEPLOYMENT")
    {
    }

    [AzureLiveTheory("AZURE_OPENAI_VISION_DEPLOYMENT", RequiresDefaultDeployment = false)]
    [InlineData((byte)255, (byte)0, (byte)0, "red")]
    [InlineData((byte)0, (byte)0, (byte)255, "blue")]
    [Trait("Category", "AzureOpenAIVision")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task Vision_IdentifiesActualImageColor(byte red, byte green, byte blue, string expectedColor)
    {
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();
        var image = new DataContent(CreateSolidColorPng(red, green, blue), "image/png") { Name = "square.png" };

        // Asking the model to name the color it sees (rather than to echo a marker word we supply in the
        // prompt) requires it to actually perceive the pixel data: a fixed/hallucinated answer, or a
        // model that silently drops the image, would fail this for at least one of the two colors below.
        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User,
                [new TextContent("What is the single dominant color of the attached image? Reply with exactly one color name."), image])],
            cancellationToken: timeout.Token);

        Assert.Contains(expectedColor, response.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] CreateSolidColorPng(byte red, byte green, byte blue)
    {
        const int size = 256;
        var raw = new byte[size * (1 + size * 3)];
        for (var row = 0; row < size; row++)
        {
            var offset = row * (1 + size * 3);
            for (var column = 0; column < size; column++)
            {
                raw[offset + 1 + column * 3] = red;
                raw[offset + 1 + column * 3 + 1] = green;
                raw[offset + 1 + column * 3 + 2] = blue;
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            zlib.Write(raw);

        using var png = new MemoryStream();
        png.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header[..4], size);
        BinaryPrimitives.WriteInt32BigEndian(header[4..8], size);
        header[8] = 8;
        header[9] = 2;
        WritePngChunk(png, "IHDR", header);
        WritePngChunk(png, "IDAT", compressed.ToArray());
        WritePngChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WritePngChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = uint.MaxValue;
        foreach (var value in typeBytes.Concat(data.ToArray()))
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(int)(crc & 1));
        }

        Span<byte> checksum = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(checksum, ~crc);
        stream.Write(checksum);
    }
}
