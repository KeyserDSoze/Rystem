using System.Collections.Concurrent;

namespace System.Reflection
{
    internal sealed class PropertyHandler
    {
        private readonly ConcurrentDictionary<string, TypeShowcase> _trees = new();
        public static PropertyHandler Instance { get; } = new();
        private PropertyHandler() { }
        public TypeShowcase GetEntity(Type type, IFurtherParameter[] furtherParameters)
        {
            string key = $"{type.FullName}_{string.Join('_', furtherParameters.Select(x => x.Key))}";
            if (!_trees.ContainsKey(key))
                _trees.TryAdd(key, new TypeShowcase(type, furtherParameters));
            return _trees[key];
        }
    }
}
