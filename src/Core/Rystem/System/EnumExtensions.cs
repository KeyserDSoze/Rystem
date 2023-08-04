namespace System
{
    public static class EnumExtensions
    {
        public static TEnum ToEnum<TEnum>(this Enum source)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), source.ToString(), true);
        }
        public static TEnum ToEnum<TEnum>(this string source)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), source, true);
        }
    }
}
