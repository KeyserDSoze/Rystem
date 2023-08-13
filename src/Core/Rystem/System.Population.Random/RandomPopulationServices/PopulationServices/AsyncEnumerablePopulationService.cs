using System.Collections;

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

            var args = new object[1] { entity! };
            var enumerable = Activator.CreateInstance(typeof(InternalAsyncEnumerable<>)
                .MakeGenericType(valueType), args)!;
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
        private sealed class InternalAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            public List<T> Items { get; }
            public InternalAsyncEnumerable(List<T> list)
            {
                Items = list;
            }
            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;
                foreach (var item in Items)
                {
                    yield return item;
                }
            }
        }
    }
}
