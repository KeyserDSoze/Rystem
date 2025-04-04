﻿using RepositoryFramework;
using RepositoryFramework.InMemory;

namespace Rystem.Localization.Test.App
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalizationFromRystem(this IServiceCollection services)
        {
            services.AddLocalizationWithRepositoryFramework<TheDictionary>(builder =>
            {
                builder.WithInMemory(name: "localization");
            },
            "localization",
            async (serviceProvider) =>
            {
                var repository = serviceProvider.GetRequiredService<IRepository<TheDictionary, string>>();
                await repository.InsertAsync("it", new TheDictionary
                {
                    Value = "Valore",
                    TheFirstPage = new TheFirstPage
                    {
                        Title = "Titolo",
                        Description = "Descrizione"
                    },
                    TheSecondPage = new TheSecondPage
                    {
                        Title = "Titolo {0}",
                    }
                });
                await repository.InsertAsync("en", new TheDictionary
                {
                    Value = "Value",
                    TheFirstPage = new TheFirstPage
                    {
                        Title = "Title",
                        Description = "Description"
                    },
                    TheSecondPage = new TheSecondPage
                    {
                        Title = "Title {0}",
                    }
                });
            });
            return services;
        }
    }
}
