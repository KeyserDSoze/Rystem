using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace System.Text.Csv
{
    internal sealed class CsvEngine
    {
        private sealed class MapHandler
        {
            public required Dictionary<string, RowHandler> Map { get; init; }
            public void Add(int index, BasePropertyNameValue basePropertyNameValue)
            {
                if (!Map.ContainsKey(basePropertyNameValue.NavigationPath))
                    Map.Add(basePropertyNameValue.NavigationPath, new RowHandler
                    {
                        Max = 1,
                        NavigationPath = basePropertyNameValue.NavigationPath,
                        Rows = []
                    });
                var map = Map[basePropertyNameValue.NavigationPath];
                if (map.Rows.Count <= index)
                    map.Rows.Add(new() { Columns = [] });
                var row = map.Rows[index];
                if (!row.Columns.ContainsKey(basePropertyNameValue.Name!))
                    row.Columns.Add(basePropertyNameValue.Name!, []);
                var listOfValues = row.Columns[basePropertyNameValue.Name!];
                listOfValues.Add(basePropertyNameValue.Value?.ToString() ?? string.Empty);
                if (listOfValues.Count > map.Max)
                    map.Max = listOfValues.Count;
            }
        }
        private sealed class RowHandler
        {
            public required string NavigationPath { get; init; }
            public required List<ColumnHandler> Rows { get; init; }
            public required int Max { get; set; }
        }
        private sealed class ColumnHandler
        {
            public required Dictionary<string, List<string>> Columns { get; init; }
        }
        public static string Convert<T>(IEnumerable<T> values, CsvEngineConfiguration<T> configuration)
        {
            var showcase = typeof(T).ToShowcase();
            var tableHandler = new MapHandler() { Map = [] };
            var counter = 0;
            foreach (var value in values)
            {
                ConvertOne(value, tableHandler, showcase.Properties, [], ref counter);
                counter++;
            }

            var header = new StringBuilder();
            foreach (var map in tableHandler.Map.Where(x => !configuration.ToAvoid.ContainsKey(x.Key)).Select(x => x.Value))
            {
                if (map.Max < 2)
                {
                    foreach (var key in map.Rows.First().Columns.Where(x => !configuration.ToAvoid.ContainsKey(x.Key)).Select(x => x.Key))
                    {
                        var name = string.IsNullOrWhiteSpace(map.NavigationPath) ? key : $"{map.NavigationPath}.{key}";
                        var value = configuration.Headers.TryGetValue(name, out var replacingName) ? replacingName :
                            (string.IsNullOrWhiteSpace(map.NavigationPath) || !configuration.UseExtendedName ? key : name);
                        header.Append(CheckIfContainsEscapeCharactersAndConfigurations(value, configuration));
                    }
                }
                else
                {
                    foreach (var key in map.Rows.First().Columns.Where(x => !configuration.ToAvoid.ContainsKey(x.Key)).Select(x => x.Key))
                        for (var i = 0; i < map.Max; i++)
                        {
                            var name = string.IsNullOrWhiteSpace(map.NavigationPath) ? key : $"{map.NavigationPath}.{key}";
                            var value = configuration.Headers.TryGetValue(name, out var replacingName) ? $"{replacingName}[{i}]" :
                                (!configuration.UseExtendedName ? map.NavigationPath : $"{map.NavigationPath}[{i}].{key}");
                            header.Append(CheckIfContainsEscapeCharactersAndConfigurations(value, configuration));
                        }
                }
            }
            var rows = new StringBuilder[counter];
            foreach (var map in tableHandler.Map.Select(x => x.Value))
            {
                var internalCounter = 0;
                foreach (var row in map.Rows.Select(x => x.Columns))
                {
                    if (rows[internalCounter] == null)
                        rows[internalCounter] = new StringBuilder();
                    var stringBuilder = rows[internalCounter];
                    foreach (var columnValues in row.Where(x => !configuration.ToAvoid.ContainsKey(x.Key)).Select(x => x.Value))
                    {
                        stringBuilder.Append(string.Join(string.Empty, columnValues.Select(x => CheckIfContainsEscapeCharactersAndConfigurations(x, configuration))));
                        for (var i = columnValues.Count; i < map.Max; i++)
                            stringBuilder.Append(configuration.Delimiter);
                    }
                    internalCounter++;
                }
            }
            var headerAsString = header.ToString();
            foreach (var delimiterAsChar in configuration.Delimiter.Reverse())
            {
                headerAsString = headerAsString.Trim(delimiterAsChar);
            }
            return $"{headerAsString}{'\n'}{string.Join('\n', rows.Select(x => x.ToString()[0..^(configuration.Delimiter.Length)]))}";
        }
        private static void ConvertOne(object? value, MapHandler tableHandler, List<BaseProperty> properties, int[] indexes, ref int counter)
        {
            foreach (var property in properties)
            {
                if (property.Type == PropertyType.Primitive || property.Type == PropertyType.Flag)
                {
                    var entry = property.NamedValue(value, indexes);
                    tableHandler.Add(counter, entry);
                }
                else if (property.Type == PropertyType.Complex)
                {
                    ConvertOne(value, tableHandler, property.Sons, indexes, ref counter);
                }
                else
                {
                    var innerIndex = 0;
                    if (property.Value(value, indexes) is IEnumerable innerValues)
                    {
                        var innerIndexes = new int[indexes.Length + 1];
                        indexes.CopyTo(innerIndexes, 0);
                        foreach (var innerValue in innerValues)
                        {
                            innerIndexes[^1] = innerIndex;
                            ConvertOne(value, tableHandler, property.Sons, innerIndexes, ref counter);
                            innerIndex++;
                        }
                    }
                }
            }
        }
        private static string CheckIfContainsEscapeCharactersAndConfigurations<T>(string value, CsvEngineConfiguration<T> configuration)
        {
            var hasDelimiter = value.Contains(configuration.Delimiter) || configuration.ForExcel;
            if (hasDelimiter && value.Contains('"'))
                value = value.Replace(QuoteCharacter, DoubleQuoteCharacter);
            return hasDelimiter ? $"\"{value}\"{configuration.Delimiter}" : $"{value}{configuration.Delimiter}";
        }
        private const string QuoteCharacter = "\"";
        private const string DoubleQuoteCharacter = "\"\"";
    }
}
