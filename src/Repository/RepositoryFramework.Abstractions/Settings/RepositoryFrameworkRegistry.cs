namespace RepositoryFramework
{
    /// <summary>
    /// Service registry of all added repository or CQRS services, singletoned and injected in dependency injection.
    /// </summary>
    public class RepositoryFrameworkRegistry
    {
        public static RepositoryFrameworkRegistry Instance { get; } = new();
        public Dictionary<string, RepositoryFrameworkService> Services { get; } = new();
        private RepositoryFrameworkRegistry() { }
        public static string ToServiceKey(Type modelType, PatternType type)
            => $"{modelType.FullName}_{type}";
        public IEnumerable<RepositoryFrameworkService> GetByModel(Type modelType)
            => Services.Select(x => x.Value).Where(x => x.ModelType == modelType);
    }
}
