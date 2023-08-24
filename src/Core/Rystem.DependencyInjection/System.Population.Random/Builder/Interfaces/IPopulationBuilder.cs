using System.Linq.Expressions;

namespace System.Population.Random
{
    public interface IPopulationBuilder<T>
    {
        IPopulationBuilder<T> WithPattern<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, params string[] regex);
        IPopulationBuilder<T> WithSpecificNumberOfElements<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, int numberOfElements);
        IPopulationBuilder<T> WithValue<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, Func<TProperty> creator);
        IPopulationBuilder<T> WithValue<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, Func<IServiceProvider, Task<TProperty>> valueRetriever);
        IPopulationBuilder<T> WithRandomValue<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> navigationPropertyPath,
            Func<IServiceProvider, Task<IEnumerable<TProperty>>> valuesRetriever);
        IPopulationBuilder<T> WithRandomValue<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath,
            Func<IServiceProvider, Task<IEnumerable<TProperty>>> valuesRetriever);
        IPopulationBuilder<T> WithAutoIncrement<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, TProperty start);
        IPopulationBuilder<T> WithImplementation<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, Type implementationType);
        IPopulationBuilder<T> WithImplementation<TProperty, TEntity>(Expression<Func<T, TProperty>> navigationPropertyPath);
        List<T> Populate(int numberOfElements = 100, int numberOfElementsWhenEnumerableIsFound = 10);
    }
}
