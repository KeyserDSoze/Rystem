namespace System
{
    public static class StringExtensions
    {
        public static string Replace(this string value, string oldValue, string newValue, int occurences)
        {
            for (int i = 0; i < occurences; i++)
            {
                int pos = value.IndexOf(oldValue);
                if (pos < 0)
                    break;
                value = $"{value[..pos]}{newValue}{value[(pos + oldValue.Length)..]}";
            }
            return value;
        }
    }
}
