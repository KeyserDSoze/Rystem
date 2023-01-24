using System.Collections.Concurrent;

namespace System.Reflection
{
    public sealed class ModelBuilder
    {
        internal static ConcurrentDictionary<string, Type> Types { get; } = new();
        private readonly string _name;
        public ModelBuilder(string name)
            => _name = name;

        private readonly List<MockedProperty> _properties = new();
        public ModelBuilder AddProperty(string name, Type type)
        {
            _properties.Add(new MockedProperty { Name = name, Type = type });
            return this;
        }
        public Type Build()
        {
            var builtType = MockedAssembly.Instance.CreateFromScratch(_name, _properties);
            Types.TryAdd(_name, builtType!);
            return builtType!;
        }
    }
}
