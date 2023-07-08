using Rystem.Content;
using Rystem.Content.Infrastructure.Storage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ContentRepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a blob storage integration to file repository.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IContentRepositoryBuilder WithInMemoryIntegration(
          this IContentRepositoryBuilder builder,
          string? name = null)
        {
            return builder.WithIntegration<InMemoryRepository>(name, ServiceLifetime.Singleton);
        }
    }
}
