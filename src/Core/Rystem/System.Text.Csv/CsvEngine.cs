using System.Collections;
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
                        Rows = new()
                    });
                var map = Map[basePropertyNameValue.NavigationPath];
                if (map.Rows.Count <= index)
                    map.Rows.Add(new() { Columns = new() });
                var row = map.Rows[index];
                if (!row.Columns.ContainsKey(basePropertyNameValue.Name!))
                    row.Columns.Add(basePropertyNameValue.Name!, new());
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
        public static string Convert<T>(IEnumerable<T> values)
        {
            var showcase = typeof(T).ToShowcase();
            var tableHandler = new MapHandler() { Map = new() };
            int counter = 0;
            foreach (var value in values)
            {
                ConvertOne(showcase.Properties, Array.Empty<int>());
                counter++;

                void ConvertOne(List<BaseProperty> properties, int[] indexes)
                {
                    foreach (var property in properties)
                    {
                        if (property.Type == PropertyType.Primitive)
                        {
                            var entry = property.NamedValue(value, indexes);
                            tableHandler.Add(counter, entry);
                        }
                        else if (property.Type == PropertyType.Complex)
                        {
                            ConvertOne(property.Sons, indexes);
                        }
                        else
                        {
                            var innerValues = property.Value(value, indexes) as IEnumerable;
                            var innerIndex = 0;
                            if (innerValues != null)
                            {
                                var innerIndexes = new int[indexes.Length + 1];
                                indexes.CopyTo(innerIndexes, 0);
                                foreach (var innerValue in innerValues)
                                {
                                    innerIndexes[innerIndexes.Length - 1] = innerIndex;
                                    ConvertOne(property.Sons, innerIndexes);
                                    innerIndex++;
                                }
                            }
                        }
                    }
                }
            }

            var header = new StringBuilder();
            foreach (var map in tableHandler.Map.Select(x => x.Value))
            {
                if (map.Max < 2)
                {
                    foreach (var key in map.Rows.First().Columns.Select(x => x.Key))
                        if (string.IsNullOrWhiteSpace(map.NavigationPath))
                            header.Append($"{key},");
                        else
                            header.Append($"{map.NavigationPath}.{key},");
                }
                else
                {
                    foreach (var key in map.Rows.First().Columns.Select(x => x.Key))
                        for (int i = 0; i < map.Max; i++)
                            header.Append($"{map.NavigationPath}[{i}].{key},");
                }
            }
            var rows = new StringBuilder[counter];
            foreach (var map in tableHandler.Map.Select(x => x.Value))
            {
                int internalCounter = 0;
                foreach (var row in map.Rows.Select(x => x.Columns))
                {
                    if (rows[internalCounter] == null)
                        rows[internalCounter] = new StringBuilder();
                    var stringBuilder = rows[internalCounter];
                    foreach (var columnValues in row.Select(x => x.Value))
                    {
                        if (stringBuilder.Length > 0)
                            stringBuilder.Append(',');
                        stringBuilder.Append(string.Join(',', columnValues.Select(x => CheckIfContainsEscapeCharacters(x))));
                        for (int i = columnValues.Count; i < map.Max; i++)
                            stringBuilder.Append(',');
                    }
                    internalCounter++;
                }
            }

            string CheckIfContainsEscapeCharacters(string value)
                => value.Contains(',') || value.Contains('"') ? $"\"{value}\"" : value;

            return $"{header.ToString().Trim(',')}{'\n'}{string.Join('\n', rows.Select(x => x.ToString()))}";
        }
    }
}