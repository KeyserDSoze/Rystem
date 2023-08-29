using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    /// <summary>
    /// Model for your repository service registry.
    /// </summary>
    public class RepositoryFrameworkService
    {
        public Type KeyType { get; }
        public Type ModelType { get; }
        public Type InterfaceType { get; internal set; } = null!;
        public Type ImplementationType { get; internal set; } = null!;
        public RepositoryMethods ExposedMethods { get; internal set; } = RepositoryMethods.All;
        public ServiceLifetime ServiceLifetime { get; internal set; }
        public PatternType Type { get; }
        public string FactoryName { get; }
        public string Key => $"{ModelType.Name}-{KeyType.Name}-{Type}-{FactoryName}";
        public RepositoryFrameworkService(Type keyType, Type modelType, PatternType type, string name)
        {
            KeyType = keyType;
            ModelType = modelType;
            Type = type;
            FactoryName = name;
        }
    }
}
