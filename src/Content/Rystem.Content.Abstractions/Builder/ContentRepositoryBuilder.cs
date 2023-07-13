using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content
{
    internal sealed class ContentRepositoryBuilder : IContentRepositoryBuilder
    {
        public IServiceCollection Services { get; }
        public ContentRepositoryBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IContentRepositoryBuilder WithIntegration<TFileRepository>(string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TFileRepository : class, IContentRepository
        {
            Services.AddFactory<IContentRepository, TFileRepository>(name, serviceLifetime);
            return this;
        }
        public IContentRepositoryBuilder WithIntegration<TFileRepository, TOptions>(Action<TOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TFileRepository : class, IContentRepository, IServiceWithOptions<TOptions>
            where TOptions : class, new()
        {
            Services.AddFactory<IContentRepository, TFileRepository, TOptions>(options, name, serviceLifetime);
            return this;
        }
    }
}
