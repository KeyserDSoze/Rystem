namespace System.Text.Csv
{
    public static class CsvEngineExtensions
    {
        public static string ToCsv<T>(this IEnumerable<T> values) 
            => CsvEngine.Convert(values);
    }
}
