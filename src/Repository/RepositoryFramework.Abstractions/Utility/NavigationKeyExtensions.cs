using System.Linq.Expressions;
using System.Reflection;

namespace RepositoryFramework
{
    public static class NavigationKeyExtensions
    {
        /// <summary>
        /// Method to retrieve the property name of the TKey used in your pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="navigationKey">navigation property to localize the TKey property in the T model.</param>
        /// <returns>PropertyInfo</returns>
        public static PropertyInfo GetPropertyBasedOnKey<T, TKey>(this Expression<Func<T, TKey>> navigationKey)
            where TKey : notnull
        {
            var keyName = navigationKey.ToString().Split('.').Last();
            var type = typeof(T);
            return type.GetProperties().First(x => x.Name == keyName);
        }
    }
}
