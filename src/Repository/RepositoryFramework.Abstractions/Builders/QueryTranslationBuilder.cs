using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    public sealed class QueryTranslationBuilder<T, TKey, TTranslated, TBuilder>
        where TKey : notnull
        where TBuilder : IRepositoryBaseBuilder<T, TKey, TBuilder>
    {
        public TBuilder Builder { get; }
        public IServiceCollection Services => Builder.Services;
        public QueryTranslationBuilder(TBuilder builder)
        {
            Builder = builder;
        }
        public QueryTranslationBuilder<T, TKey, TTranslated, TBuilder> WithKey<TProperty, TTranslatedProperty>(
            Expression<Func<TKey, TProperty>> property,
            Expression<Func<TTranslated, TTranslatedProperty>> translatedProperty)
        {
            var propertyValue = property.GetPropertyFromExpression()!;
            var translatedPropertyValue = translatedProperty.GetPropertyFromExpression()!;
            var compiledProperty = property.Compile();
            var compiledTranslatedProperty = translatedProperty.Compile();
            RepositoryMapper<T, TKey, TTranslated>.Instance.KeyProperties.Add(
               new RepositoryMapper<T, TKey, TTranslated>.RepositoryKeyMapperProperty(
                   translatedPropertyValue,
                   x => compiledProperty.Invoke(x)!,
                   propertyValue != null ? (x, value) => propertyValue.SetValue(x, value) : null,
                   x => compiledTranslatedProperty.Invoke(x)!,
                   (x, value) => translatedPropertyValue.SetValue(x, value)
                   ));
            return this;
        }
        public QueryTranslationBuilder<T, TKey, TTranslated, TBuilder> With<TProperty, TTranslatedProperty>(
            Expression<Func<T, TProperty>> property,
            Expression<Func<TTranslated, TTranslatedProperty>> translatedProperty)
        {
            var propertyValue = property.GetPropertyFromExpression()!;
            var translatedPropertyValue = translatedProperty.GetPropertyFromExpression()!;
            var compiledProperty = property.Compile();
            var compiledTranslatedProperty = translatedProperty.Compile();
            RepositoryMapper<T, TKey, TTranslated>.Instance.Properties.Add(
                new RepositoryMapper<T, TKey, TTranslated>.RepositoryMapperProperty(
                    x => compiledProperty.Invoke(x)!,
                    (x, value) => propertyValue.SetValue(x, value),
                    x => compiledTranslatedProperty.Invoke(x)!,
                    (x, value) => translatedPropertyValue.SetValue(x, value)
                    ));
            FilterTranslation<T, TKey>.Instance.With(property, translatedProperty);
            return this;
        }
        public QueryTranslationBuilder<T, TKey, TTranslated, TBuilder> WithSamePorpertiesName()
        {
            var translatedProperties = typeof(TTranslated).GetProperties();
            foreach (var property in typeof(T).GetProperties())
            {
                var translatedProperty = translatedProperties.FirstOrDefault(x => x.Name == property.Name);
                if (translatedProperty != null)
                {
                    RepositoryMapper<T, TKey, TTranslated>.Instance.Properties.Add(
                        new RepositoryMapper<T, TKey, TTranslated>.RepositoryMapperProperty(
                        x => property.GetValue(x)!,
                        (x, value) => property.SetValue(x, value),
                        x => translatedProperty.GetValue(x)!,
                        (x, value) => translatedProperty.SetValue(x, value)
                        ));
                }
            }
            return this;
        }
        public QueryTranslationBuilder<T, TKey, TFurtherTranslated, TBuilder> AndTranslate<TFurtherTranslated>()
            => Builder.Translate<TFurtherTranslated>();
    }
}
