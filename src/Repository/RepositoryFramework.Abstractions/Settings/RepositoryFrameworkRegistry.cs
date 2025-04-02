namespace RepositoryFramework
{
    /// <summary>
    /// Service registry of all added repository or CQRS services, singletoned and injected in dependency injection.
    /// </summary>
    public class RepositoryFrameworkRegistry
    {
        public Dictionary<string, RepositoryFrameworkService> Services { get; } = new();
        public static string ToServiceKey(Type modelType, PatternType type, string name)
            => $"{(modelType.IsGenericType ? $"{modelType.FullName}_{string.Join('_', modelType.GetGenericArguments().Select(x => x.FullName))}" : modelType.FullName)}_{type}_{name}";
        public IEnumerable<RepositoryFrameworkService> GetByModel(Type modelType)
            => Services.Select(x => x.Value).Where(x => x.ModelType == modelType);
    }
}
