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
        public bool IsNotExposable { get; internal set; }
        public ServiceLifetime ServiceLifetime { get; internal set; }
        public PatternType Type { get; }
        public RepositoryFrameworkService(Type keyType, Type modelType, PatternType type)
        {
            KeyType = keyType;
            ModelType = modelType;
            Type = type;
        }
    }
}
