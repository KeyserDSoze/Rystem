using Rystem.Content;
using Rystem.Content.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ContentRepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a blob storage integration to file repository.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionSettings"></param>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        public static IContentRepositoryBuilder WithSharepointIntegration(
          this IContentRepositoryBuilder builder,
          Action<SharepointConnectionSettings> connectionSettings,
          string? name = null,
          ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            name ??= string.Empty;
            var options = new SharepointConnectionSettings();
            connectionSettings.Invoke(options);
            SharepointServiceClientFactory.Instance.Add(options, name);
            return builder.WithIntegration<SharepointRepository>(name, serviceLifetime);
        }
    }
}
