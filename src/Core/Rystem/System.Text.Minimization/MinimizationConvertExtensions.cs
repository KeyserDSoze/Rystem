namespace System.Text.Minimization
{
    public static class MinimizationConvertExtensions
    {
        public static string ToMinimize<T>(this T data, char? startSeparator = null) 
            => Serializer.Instance.Serialize(data!.GetType(), data, startSeparator == null ? int.MaxValue : (int)startSeparator);

        public static T FromMinimization<T>(this string value, char? startSeparator = null) 
            => (T)Serializer.Instance.Deserialize(typeof(T), value, startSeparator == null ? int.MaxValue : (int)startSeparator);
    }
}
