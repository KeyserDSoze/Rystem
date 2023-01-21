using System.Text.Json;

namespace System.Text
{
    public static class Base45Extensions
    {
        private const string Label = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
        private static readonly Dictionary<int, string> Values = new();
        private static readonly Dictionary<char, int> ValuesAsString = new();
        private const int O45X45 = 45 * 45;
        static Base45Extensions()
        {
            int count = 0;
            foreach (var l in Label)
            {
                Values.Add(count++, l.ToString());
                ValuesAsString.Add(l, count - 1);
            }
        }
        public static string ToBase45(this string value)
        {
            StringBuilder stringBuilder = new();
            var bytes = Encoding.UTF8.GetBytes(value);
            for (int i = 0; i < bytes.Length; i += 2)
            {
                var values = bytes.Skip(i).Take(2).ToList();
                int? second = default;
                if (values.Count > 1)
                    second = values[1];
                var returnedValue = Calculate(values[0], second);
                stringBuilder.Append($"{Values[returnedValue.Item1]}{Values[returnedValue.Item2]}{(returnedValue.Item3.HasValue ? Values[returnedValue.Item3.Value] : string.Empty)}");
            }
            return stringBuilder.ToString();
        }
        public static string ToBase45<T>(this T entity) => entity.ToJson().ToBase45();
        public static string FromBase45(this string encondedValue)
        {
            int remainder = encondedValue.Length % 3;
            if (remainder == 1)
                throw new ArgumentException($"String length is not correct. A possible length is a multiple of 3 or a multiple of 3 minus 1. The actual length is {encondedValue.Length}");
            StringBuilder stringBuilder = new();
            List<int> values = new();
            foreach (var x in encondedValue)
            {
                if (ValuesAsString.ContainsKey(x))
                    values.Add(ValuesAsString[x]);
                else
                    throw new ArgumentException($"Character {x} doesn't recognize as valid character.");
            }
            for (int i = 0; i < values.Count; i += 3)
            {
                int? e = default;
                if (i + 2 < values.Count)
                    e = values[i + 2];
                var returnedValue = Calculate(values[i], values[i + 1], e);
                stringBuilder.Append($"{(returnedValue.Item2.HasValue ? new string((char)returnedValue.Item2.Value, 1) : string.Empty)}{(char)returnedValue.Item1}");
            }
            return stringBuilder.ToString();
        }
        public static T FromBase45<T>(this string encodedValue) => encodedValue.FromBase45().FromJson<T>();
        private static Tuple<int, int, int?> Calculate(int a, int? b = default)
        {
            int? e = default;
            int value = !b.HasValue ? a : a * 256 + b.Value;
            if (b.HasValue)
            {
                e = value / O45X45;
                value %= O45X45;
            }
            int d = value / 45;
            value %= 45;
            return new Tuple<int, int, int?>(value, d, e);
        }
        private static Tuple<int, int?> Calculate(int c, int d, int? e = default)
        {
            int? b = default;
            int value = c + d * 45 + (e.HasValue ? e.Value * O45X45 : 0);
            if (e.HasValue)
            {
                b = value / 256;
                value %= 256;
            }
            return new Tuple<int, int?>(value, b);
        }
    }
}