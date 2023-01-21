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
            public LambdaExpression? Transform(string? serialized)
            {
                if (string.IsNullOrWhiteSpace(serialized))
                    return null;

                foreach (var translation in Translations)
                {
                    if (serialized.EndsWith(translation.EndWith))
                    {
                        var place = serialized.LastIndexOf(translation.EndWith);
                        if (place > -1)
                            serialized = serialized.Remove(place, translation.EndWith.Length).Insert(place, translation.Value);
                    }
                    var list = translation.Key.Matches(serialized);
                    for (var i = 0; i < list.Count; i++)
                    {
                        var match = list[i];
                        serialized = serialized.Replace(match.Value, $"{translation.Value}{match.Value.Last()}");
                    }
                }
                var deserialized = serialized.DeserializeAsDynamic(To);
                return deserialized;
            }
        }
        public static FilterTranslation<T, TKey> Instance { get; } = new();
        private FilterTranslation() { }
        private sealed record Translation(Regex Key, string EndWith, string Value);
        private readonly Dictionary<string, TranslationWrapper> _translations = new();
        private static Regex VariableName(string prefix) => new($@"\.{prefix}[^a-zA-Z0-9@_]{{1}}");
        public void With<TTranslated, TProperty, TTranslatedProperty>(Expression<Func<T, TProperty>> property, Expression<Func<TTranslated, TTranslatedProperty>> translatedProperty)
        {
            Setup<TTranslated>();
            var translatedName = typeof(TTranslated).FullName!;
            var propertyName = string.Join(".", property.ToString().Split('.').Skip(1));
            var translatedPropertyName = $".{string.Join(".", translatedProperty.ToString().Split('.').Skip(1))}";
            _translations[translatedName].Translations.Add(new Translation(VariableName(propertyName), $".{propertyName}", translatedPropertyName));
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
                        operation.Operation, operation.Value != null ? long.Parse(operation.Value) : null));
                    }
                    else
                    {
                        filter.Operations.Add(new LambdaFilterOperation(
                        operation.Operation,
                        translation.Transform(operation.Value)));
                    }
                }
                multipleFilterExpression.Filters.Add(translationKeyValue.Key, filter);
            }
            return multipleFilterExpression;
        }
    }
}
