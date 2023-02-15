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
        public ModelBuilder AddProperty<T>(string name) 
            => AddProperty(name, typeof(T));
        public ModelBuilder AddProperty(string name, Type type)
        {
            _properties.Add(new MockedProperty { Name = name, Type = type });
            return this;
        }
        private Type? _parentType;
        public ModelBuilder AddParent(Type parentType)
        {
            _parentType = parentType;
            return this;
        }
        public ModelBuilder AddParent<T>() 
            => AddParent(typeof(T));
        public Type Build()
        {
            var builtType = MockedAssembly.Instance.CreateFromScratch(_name, _parentType, _properties);
            Types.TryAdd(_name, builtType!);
            return builtType!;
        }
    }
}
