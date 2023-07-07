using Microsoft.Extensions.DependencyInjection.Extensions;
using Rystem.Content;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add file repository
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <returns></returns>
        public static IContentRepositoryBuilder AddFileRepository(this IServiceCollection services)
        {
            services.TryAddTransient<IContentRepositoryFactory, ContentRepositoryFactory>();
            return new ContentRepositoryBuilder(services);
        }
    }
}
