using Rystem.Content;
using Rystem.Content.Infrastructure.Storage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ContentRepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a file storage integration to content repository.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionSettings"></param>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns>IContentRepositoryBuilder</returns>
        public static async Task<IContentRepositoryBuilder> WithFileStorageIntegrationAsync(
          this IContentRepositoryBuilder builder,
          Action<FileStorageConnectionSettings> connectionSettings,
          string? name = null,
          ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            await builder
               .WithIntegrationAsync<FileStorageRepository, FileStorageConnectionSettings, FileServiceClientWrapper>(connectionSettings, name, serviceLifetime)
               .NoContext();
            return builder;
        }
        /// <summary>
        /// Add a file storage integration to content repository.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionSettings"></param>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns>IContentRepositoryBuilder</returns>
        public static IContentRepositoryBuilder WithFileStorageIntegration(
          this IContentRepositoryBuilder builder,
          Action<FileStorageConnectionSettings> connectionSettings,
          string? name = null,
          ServiceLifetime serviceLifetime = ServiceLifetime.Transient) 
            => builder.WithFileStorageIntegrationAsync(connectionSettings, name, serviceLifetime).ToResult();
    }
}
