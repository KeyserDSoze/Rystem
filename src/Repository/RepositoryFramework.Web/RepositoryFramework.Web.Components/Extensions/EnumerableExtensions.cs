using System.Collections;
using System.Linq.Dynamic.Core;
using System.Reflection;
using RepositoryFramework.Web.Components.Business.Language;

namespace RepositoryFramework.Web.Components.Extensions
{
    internal static class EnumerableExtensions
    {
        public static string EnumerableCountAsString(this object? value, BaseProperty baseProperty, ILocalizationHandler localizationHandler)
        {
            return localizationHandler.Get(LanguageLabel.ShowItems, EnumerableCount(value));

            int EnumerableCount(object? entity)
            {
                var response = Try.WithDefaultOnCatch(() => baseProperty.Value(entity, null));
                if (response.Exception != null)
                    return -1;
                var items = response.Entity;
                if (items == null)
                    return 0;
                else if (items is IList list)
                    return list.Count;
                else if (items is IQueryable queryable)
                    return queryable.Count();
                else if (items.GetType().IsArray)
                    return ((dynamic)items).Length;
                else if (items is IEnumerable enumerable)
                    return enumerable.AsQueryable().Count();
                return 0;
            }
        }
    }
}
