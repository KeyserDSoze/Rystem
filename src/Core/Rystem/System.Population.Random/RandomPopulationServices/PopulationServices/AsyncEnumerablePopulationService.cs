using System.Collections;
using System.Reflection;

namespace System.Population.Random
{
    internal class AsyncEnumerablePopulationService : IRandomPopulationService
    {
        public int Priority => 2;
        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
        {
            var valueType = options.Type.GetGenericArguments().First();
            var listType = typeof(List<>).MakeGenericType(valueType);
            var entity = Activator.CreateInstance(listType)! as IList;
            for (var i = 0; i < options.NumberOfEntities; i++)
            {
                var newValue = options.PopulationService.Construct(settings, options.Type.GetGenericArguments().First(),
                    options.NumberOfEntities, options.TreeName, string.Empty);
                entity!.Add(newValue);
            }
            var enumerable = typeof(AsyncEnumerable<>)
                .MakeGenericType(valueType)
                .CreateInstance(entity!);
            return enumerable;
        }

        public bool IsValid(Type type)
        {
            if (!type.IsArray)
            {
                var interfaces = type.GetInterfaces();
                if (type.Name.Contains("IAsyncEnumerable`1") || interfaces.Any(x => x.Name.Contains("IAsyncEnumerable`1")))
                    return true;
            }
            return false;
        }
        private sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly List<T> _list;
            public AsyncEnumerable(List<T> list)
            {
                _list = list;
            }
            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;
                foreach (var item in _list)
                {
                    yield return item;
                }
            }
        }
    }
}
