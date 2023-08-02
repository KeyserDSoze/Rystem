namespace System.IO
{
    public static class StreamExtensions
    {
        public static Stream ToStream(this byte[] bytes)
            => new MemoryStream(bytes)
            {
                Position = 0
            };
        public static byte[] ToArray(this Stream stream)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
        public static async Task<byte[]> ToArrayAsync(this Stream stream)
        {
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream).NoContext();
            return memoryStream.ToArray();
        }
    }
}
