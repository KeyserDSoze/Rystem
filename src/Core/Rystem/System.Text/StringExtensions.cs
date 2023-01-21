namespace System.Text
{
    public static class StringExtensions
    {
        public static string ToUpperCaseFirst(this string value)
            => string.IsNullOrWhiteSpace(value) ? value : $"{value.FirstOrDefault().ToString().ToUpper()}{value[1..].ToLower()}";
        public static Stream ToStream(this byte[] bytes)
            => new MemoryStream(bytes)
            {
                Position = 0
            };
        public static Task<string> ConvertToStringAsync(this Stream entity)
        {
            if (entity.CanSeek)
                entity.Position = 0;
            using StreamReader streamReader = new(entity);
            return streamReader.ReadToEndAsync();
        }
        public static string ConvertToString(this Memory<byte> buffer, EncodingType type = EncodingType.UTF8)
        {
#pragma warning disable SYSLIB0001 // Type or member is obsolete
            return type switch
            {
                EncodingType.UTF8 => Encoding.UTF8.GetString(buffer.Span),
                EncodingType.UTF7 => Encoding.UTF7.GetString(buffer.Span),
                EncodingType.UTF32 => Encoding.UTF32.GetString(buffer.Span),
                EncodingType.Latin1 => Encoding.Latin1.GetString(buffer.Span),
                EncodingType.ASCII => Encoding.ASCII.GetString(buffer.Span),
                EncodingType.BigEndianUnicode => Encoding.BigEndianUnicode.GetString(buffer.Span),
                _ => Encoding.Default.GetString(buffer.Span),
            };
#pragma warning restore SYSLIB0001 // Type or member is obsolete
        }
        public static async IAsyncEnumerable<string> ReadLinesAsync(this Stream entity)
        {
            if (entity.CanSeek)
                entity.Position = 0;
            using StreamReader streamReader = new(entity);
            while (!streamReader.EndOfStream)
                yield return (await streamReader.ReadLineAsync().NoContext())!;
        }
        public static string ConvertToString(this Stream entity)
            => ConvertToStringAsync(entity).ToResult();
        public static Stream ToStream(this string entity)
            => new MemoryStream(entity.ToByteArray());

        public static string ConvertToString(this byte[] entity, EncodingType type = EncodingType.UTF8)
        {
#pragma warning disable SYSLIB0001 // Type or member is obsolete
            return type switch
            {
                EncodingType.UTF8 => Encoding.UTF8.GetString(entity),
                EncodingType.UTF7 => Encoding.UTF7.GetString(entity),
                EncodingType.UTF32 => Encoding.UTF32.GetString(entity),
                EncodingType.Latin1 => Encoding.Latin1.GetString(entity),
                EncodingType.ASCII => Encoding.ASCII.GetString(entity),
                EncodingType.BigEndianUnicode => Encoding.BigEndianUnicode.GetString(entity),
                _ => Encoding.Default.GetString(entity),
            };
#pragma warning restore SYSLIB0001 // Type or member is obsolete
        }
        public static byte[] ToByteArray(this string entity, EncodingType type = EncodingType.UTF8)
        {
#pragma warning disable SYSLIB0001 // Type or member is obsolete
            return type switch
            {
                EncodingType.UTF8 => Encoding.UTF8.GetBytes(entity),
                EncodingType.UTF7 => Encoding.UTF7.GetBytes(entity),
                EncodingType.UTF32 => Encoding.UTF32.GetBytes(entity),
                EncodingType.Latin1 => Encoding.Latin1.GetBytes(entity),
                EncodingType.ASCII => Encoding.ASCII.GetBytes(entity),
                EncodingType.BigEndianUnicode => Encoding.BigEndianUnicode.GetBytes(entity),
                _ => Encoding.Default.GetBytes(entity),
            };
#pragma warning restore SYSLIB0001 // Type or member is obsolete
        }
        public static bool ContainsAtLeast(this string value, int count, char contained)
        {
            int counter = 0;
            foreach (char c in value)
            {
                if (contained == c)
                    counter++;
                if (counter >= count)
                    return true;
            }
            return false;
        }
    }
}