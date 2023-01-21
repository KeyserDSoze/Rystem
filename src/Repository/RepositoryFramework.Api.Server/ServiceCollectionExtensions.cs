using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add api interfaces from repository framework. You can add configuration for Swagger, Identity in swagger and documentation.
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IServiceCollection</returns>
        public static IApiBuilder AddApiFromRepositoryFramework(this IServiceCollection services)
            => new ApiBuilder(services);
    }
}
