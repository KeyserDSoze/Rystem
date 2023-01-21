namespace RepositoryFramework
{
    internal sealed class ApiAuthorization
    {
        public Dictionary<RepositoryMethods, List<string>> Policies { get; } = new();
        public string[]? GetPolicy(RepositoryMethods method)
        {
            if (Policies.ContainsKey(method))
                return Policies[method].ToArray();
            if (Policies.ContainsKey(RepositoryMethods.All))
                return Policies[RepositoryMethods.All].ToArray();
            return null!;
        }
    }
}
