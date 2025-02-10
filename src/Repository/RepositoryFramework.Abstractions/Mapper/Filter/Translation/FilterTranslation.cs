using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace RepositoryFramework
{
    internal sealed class FilterTranslation<T, TKey> : IRepositoryFilterTranslator<T, TKey>
        where TKey : notnull
    {
        private sealed record TranslationWrapper(Type From, Type To)
        {
            public List<Translation> Translations { get; internal set; } = new();
            private static readonly Regex s_regex = new(@"\.[^(]*\([^=]*=>[^.]*.");
            private static readonly Regex s_findTheX = new(@"(\(|\A)[^=]*=>");
            public LambdaExpression? Transform(string? serialized)
            {
                if (string.IsNullOrWhiteSpace(serialized))
                    return null;
                var xs = s_findTheX.Matches(serialized).Select(x => x.Value.Split('=').First().Split('(').Last().Trim()).ToList();
                foreach (var translation in Translations)
                {
                    foreach (var theX in xs)
                    {
                        if (!translation.IsEnumerable && serialized.EndsWith(translation.EndWith!))
                        {
                            var place = serialized.LastIndexOf(translation.EndWith!);
                            if (place > -1)
                                serialized = serialized.Remove(place, translation.EndWith!.Length).Insert(place, translation.Value);
                        }
                        var regex = new Regex($"{theX}{translation.Key}");
                        var list = regex.Matches(serialized);
                        for (var i = 0; i < list.Count; i++)
                        {
                            var match = list[i];
                            if (translation.IsEnumerable)
                            {
                                var operatorValue = s_regex.Match(match.Value[2..]).Value;
                                serialized = serialized.Replace(match.Value, $"{theX}{translation.Value}{match.Value.Last()}");
                                serialized = serialized.Replace(FirstReplacerWithDot, operatorValue);
                            }
                            else
                            {
                                serialized = serialized.Replace(match.Value, $"{theX}{translation.Value}{match.Value.Last()}");
                            }
                        }
                    }
                }
                var deserialized = serialized.DeserializeAsDynamic(To);
                return deserialized;
            }
        }
        public static FilterTranslation<T, TKey> Instance { get; } = new();
        private FilterTranslation() { }
        private sealed record Translation(string Key, string EndWith, string Value, bool IsEnumerable);
        private readonly Dictionary<string, TranslationWrapper> _translations = new();
        private static string VariableName(string prefix) => $@"\.{prefix}[^a-zA-Z0-9@_]{{1}}";
        private static string VariableForEnumerableName(string prefix) => $@"\.{prefix.Replace(First, "[^(]*\\([^=]*=>[^.]*")}[^a-zA-Z0-9@_]{{1}}";
        private const string First = "First()";
        private const string FirstReplacer = "First__Replacer()";
        private const string FirstReplacerWithDot = ".First__Replacer().";
        public void With<TTranslated, TProperty, TTranslatedProperty>(Expression<Func<T, TProperty>> property, Expression<Func<TTranslated, TTranslatedProperty>> translatedProperty)
        {
            Setup<TTranslated>();
            var translatedName = typeof(TTranslated).FullName!;
            var propertyName = string.Join(".", property.ToString().Split('.').Skip(1));
            if (propertyName.Contains(First))
            {
                var translatedPropertyName = $".{string.Join(".", translatedProperty.ToString().Split('.').Skip(1))}".Replace(First, FirstReplacer);
                _translations[translatedName].Translations.Add(new Translation(VariableForEnumerableName(propertyName), string.Empty, translatedPropertyName, true));
            }
            else
            {
                var translatedPropertyName = $".{string.Join(".", translatedProperty.ToString().Split('.').Skip(1))}";
                _translations[translatedName].Translations.Add(new Translation(VariableName(propertyName), $".{propertyName}", translatedPropertyName, false));
            }
            _translations[translatedName].Translations = _translations[translatedName].Translations.OrderByDescending(x => x.Value.Length).ToList();
        }
        public void Setup<TTranslated>()
        {
            var translatedName = typeof(TTranslated).FullName!;
            if (!_translations.ContainsKey(translatedName))
                _translations.Add(translatedName, new(typeof(T), typeof(TTranslated)));
        }
        public IFilterExpression Transform(SerializableFilter serializableFilter)
        {
            MultipleFilterExpression multipleFilterExpression = new();
            foreach (var translationKeyValue in _translations)
            {
                var translation = translationKeyValue.Value;
                FilterExpression filter = new();
                foreach (var operation in serializableFilter.Operations)
                {
                    if (operation.Operation == FilterOperations.Top || operation.Operation == FilterOperations.Skip)
                    {
                        filter.Operations.Add(new ValueFilterOperation(
                        operation.Operation, FilterRequest.Entity, operation.Value != null ? long.Parse(operation.Value) : null));
                    }
                    else
                    {
                        filter.Operations.Add(new LambdaFilterOperation(
                        operation.Operation,
                        operation.Request,
                        translation.Transform(operation.Value)));
                    }
                }
                multipleFilterExpression.Filters.Add(translationKeyValue.Key, filter);
            }
            return multipleFilterExpression;
        }
    }
}
