using Fare;
using System.Linq;

namespace System.Population.Random
{
    internal class RegexService : IRegexService
    {
        public dynamic GetRandomValue(Type type, string[] pattern)
        {
            if (pattern.Length > 0)
            {
                var xeger = new Xeger(pattern.First());
                var generatedString = xeger.Generate();
                if (type.Name.Contains("Nullable`1"))
                    type = type.GenericTypeArguments[0];
                if (type == typeof(Guid))
                    return Guid.Parse(generatedString);
                else if (type == typeof(DateTimeOffset))
                    return DateTimeOffset.Parse(generatedString);
                else if (type == typeof(TimeSpan))
                    return new TimeSpan(long.Parse(generatedString));
                else if (type == typeof(nint))
                    return nint.Parse(generatedString);
                else if (type == typeof(nuint))
                    return nuint.Parse(generatedString);
                else if (type == typeof(Range))
                {
                    var first = int.Parse(generatedString);
                    xeger = new Xeger(pattern.Last());
                    generatedString = xeger.Generate();
                    var second = int.Parse(generatedString);
                    if (first > second)
                        (second, first) = (first, second);
                    if (first == second)
                        second++;
                    return new Range(new Index(first), new Index(second));
                }
                else
                    return Convert.ChangeType(generatedString, type);
            }
            return null!;
        }
    }
}