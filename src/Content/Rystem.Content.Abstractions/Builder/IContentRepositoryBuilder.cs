using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content
{
    public interface IContentRepositoryBuilder
    {
        IServiceCollection Services { get; }
        /// <summary>
        /// Add content repository integration
        /// </summary>
        /// <typeparam name="TFileRepository"></typeparam>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        IContentRepositoryBuilder WithIntegration<TFileRepository>(string? name = null, ServiceLifetime serviceLifetime = ServiceLifetime.Transient) where TFileRepository : class, IContentRepository;
        /// <summary>
        /// Add content repository integration
        /// </summary>
        /// <typeparam name="TFileRepository"></typeparam>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="options"></param>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        IContentRepositoryBuilder WithIntegration<TFileRepository, TOptions>(
            Action<TOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TFileRepository : class, IContentRepository, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new();
        /// <summary>
        /// Add content repository integration
        /// </summary>
        /// <typeparam name="TFileRepository"></typeparam>
        /// <typeparam name="TOptions"></typeparam>
        /// <typeparam name="TConnection"></typeparam>
        /// <param name="options"></param>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        Task<IContentRepositoryBuilder> WithIntegrationAsync<TFileRepository, TOptions, TConnection>(
            Action<TOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TFileRepository : class, IContentRepository, IServiceWithFactoryWithOptions<TConnection>
            where TOptions : class, IOptionsBuilderAsync<TConnection>, new()
            where TConnection : class, IFactoryOptions;
    }
}
