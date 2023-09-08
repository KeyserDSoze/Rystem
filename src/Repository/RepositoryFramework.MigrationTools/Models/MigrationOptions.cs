using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Migration
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed", Justification = "It's not used but it's needed for the return methods that use this class.")]
    public class MigrationOptions<T, TKey> : IFactoryOptions
        where TKey : notnull
    {
        public int NumberOfConcurrentInserts { get; set; } = 10;
        public string SourceFactoryName { get; set; } = null!;
        public string DestinationFactoryName { get; set; } = null!;
    }
}
