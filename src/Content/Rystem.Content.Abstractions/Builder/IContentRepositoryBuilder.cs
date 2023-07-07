using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content
{
    public interface IContentRepositoryBuilder
    {
        IServiceCollection Services { get; }
        /// <summary>
        /// Add file repository integration
        /// </summary>
        /// <typeparam name="TFileRepository"></typeparam>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        IContentRepositoryBuilder WithIntegration<TFileRepository>(string? name = null, ServiceLifetime serviceLifetime = ServiceLifetime.Transient) where TFileRepository : class, IContentRepository;
    }
}
