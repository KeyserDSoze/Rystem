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
        public List<string> Policies { get; } = new();
        public string Key => $"{(ModelType.IsGenericType ? $"{ModelType.Name}_{string.Join('_', ModelType.GetGenericArguments().Select(x => x.Name))}" : ModelType.Name)}-{(KeyType.IsGenericType ? $"{KeyType.Name}_{string.Join('_', KeyType.GetGenericArguments().Select(x => x.Name))}" : KeyType.Name)}-{Type}-{FactoryName}";
        public RepositoryFrameworkService(Type keyType, Type modelType, PatternType type, string name)
        {
            KeyType = keyType;
            ModelType = modelType;
            Type = type;
            FactoryName = name;
        }
    }
}
