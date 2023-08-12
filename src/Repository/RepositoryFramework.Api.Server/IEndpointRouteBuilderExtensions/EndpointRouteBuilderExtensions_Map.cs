using System.Linq.Dynamic.Core;
using System.Population.Random;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EndpointRouteBuilderExtensions
    {
        private static readonly ApisMap s_map = new();
        private static IEndpointRouteBuilder AddMap<T, TKey>(this IEndpointRouteBuilder app,
            PatternType patternType)
        {
            return app;
        }
        private static object? PopulateRandomObject(this IServiceProvider serviceProvider, Type type)
        {
            return typeof(EndpointRouteBuilderExtensions)
                        .GetMethod(nameof(PopulateRandom), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(type)
                        .Invoke(null, new object[] { serviceProvider });
        }
        private static T PopulateRandom<T>(this IServiceProvider serviceProvider)
        {
            var populationService = serviceProvider.GetService<IPopulation<T>>()!;
            return populationService.Populate(1, 1).First();
        }
        private static string? s_mapAsJson;
        private static bool s_mapAlreadyAdded = false;
        private static void AddApiMap(this IEndpointRouteBuilder app)
        {
            if (!s_mapAlreadyAdded)
            {
                s_mapAlreadyAdded = true;
                Try.WithDefaultOnCatch(() =>
                {
                    app
                        .MapGet("Repository/Map/All", () => s_mapAsJson ??= s_map.ToJson())
                        .WithTags("_RepositoryMap");
                });
            }
        }
    }
}
