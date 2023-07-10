using Rystem.Content;
using Rystem.Content.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ContentRepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a sharepoint storage integration to content repository.
        /// Please use an App Registration with Permission Type: Application and Permissions: Files.ReadWrite.All or Sites.ReadWrite.All.
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
