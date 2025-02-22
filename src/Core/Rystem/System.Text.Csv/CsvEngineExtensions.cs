namespace System.Text.Csv
{
    public static class CsvEngineExtensions
    {
        public static string ToCsv<T>(this IEnumerable<T> values, Action<CsvEngineConfiguration<T>>? configuration = null)
        {
            var configurationInstance = new CsvEngineConfiguration<T>();
            configuration?.Invoke(configurationInstance);
            return CsvEngine.Convert(values, configurationInstance);
        }
        public static string ToCsv<T>(this IEnumerable<T> values, CsvEngineConfiguration<T> configuration)
            => CsvEngine.Convert(values, configuration);
    }
}
