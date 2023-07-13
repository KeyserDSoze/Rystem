using Microsoft.Extensions.DependencyInjection.Extensions;
using Rystem.Content;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add content repository
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <returns></returns>
        public static IContentRepositoryBuilder AddContentRepository(this IServiceCollection services)
        {
            return new ContentRepositoryBuilder(services);
        }
    }
}
