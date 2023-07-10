using Rystem.Content;
using Rystem.Content.Infrastructure.Storage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ContentRepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a blob storage integration to content repository.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionSettings"></param>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        public static IContentRepositoryBuilder WithBlobStorageIntegration(
          this IContentRepositoryBuilder builder,
          Action<BlobStorageConnectionSettings> connectionSettings,
          string? name = null,
          ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            name ??= string.Empty;
            var options = new BlobStorageConnectionSettings();
            connectionSettings.Invoke(options);
            BlobServiceClientFactory.Instance.Add(options, name);
            return builder.WithIntegration<BlobStorageRepository>(name, serviceLifetime);
        }
    }
}
