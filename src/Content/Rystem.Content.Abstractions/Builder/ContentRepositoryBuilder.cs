﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rystem.Content
{
    internal sealed class ContentRepositoryBuilder : IContentRepositoryBuilder
    {
        public IServiceCollection Services { get; }
        public ContentRepositoryBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IContentRepositoryBuilder WithIntegration<TFileRepository>(string? name = null, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TFileRepository : class, IContentRepository
        {
            name ??= string.Empty;
            Services.AddService<TFileRepository>(serviceLifetime);
            Services.AddService<IContentRepository, TFileRepository>(serviceLifetime);
            ContentRepositoryFactoryWrapper.Instance.Creators.Add(name,
                (serviceProvider) =>
                {
                    var repository = serviceProvider.GetService<TFileRepository>() ?? throw new ArgumentException($"File repository {name} is not installed.");
                    repository.SetName(name);
                    return repository;
                });
            Services.TryAddTransient<IContentRepositoryFactory, ContentRepositoryFactory>();
            return this;
        }
    }
}