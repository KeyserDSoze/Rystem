using System.IO;
using System.Linq.Dynamic.Core;
using System.Population.Random;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RepositoryFramework;
using RepositoryFramework.Abstractions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EndpointRouteBuilderExtensions
    {
        private static readonly ApisMap s_map = new();
        private static IEndpointRouteBuilder AddMap(this IEndpointRouteBuilder app,
            string uri,
            Type model,
            Type key,
            RepositoryMethods method,
            string currentName,
            RepositoryFrameworkService service,
            ApiAuthorization? authorization)
        {
            _ = typeof(EndpointRouteBuilderExtensions).GetMethod(nameof(AddMapAsGenerics),
                    BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(model, key)
                        .Invoke(null, new object[] { app, uri, method, currentName, service, authorization! });
            return app;
        }
        private static IEndpointRouteBuilder AddMapAsGenerics<T, TKey>(this IEndpointRouteBuilder app,
            string uri,
            RepositoryMethods method,
            string currentName,
            RepositoryFrameworkService service,
            ApiAuthorization authorization)
            where TKey : notnull
        {
            var api = s_map.Apis.FirstOrDefault(x => x.Name == currentName && x.FactoryName == service.FactoryName);
            if (api == null)
            {
                api = new ApiMap
                {
                    Name = currentName,
                    FactoryName = service.FactoryName,
                    FullName = service.Key,
                    PatternType = service.Type.ToString(),
                    KeyIsJsonable = KeySettings<TKey>.Instance.IsJsonable,
                    Requests = new()
                };
                s_map.Apis.Add(api);
            }
            if (api.Key == null || api.Model == null)
            {
                var exampleProvider = app.ServiceProvider.GetService<IRepositoryExamples<T, TKey>>();
                if (exampleProvider != null)
                {
                    api.Key = exampleProvider.Key;
                    api.Model = exampleProvider.Entity;
                }
                else
                {
                    api.Key = PopulateRandom<TKey>();
                    api.Model = PopulateRandom<T>();
                }
            }
            var request = new RequestApiMap
            {
                HasStream = false,
                HttpMethod = api.KeyIsJsonable ? HttpMethods.Get : HttpMethods.Post,
                IsAuthenticated = authorization?.GetPolicy(method) != null,
                Policies = authorization?.GetPolicy(method),
                IsAuthorized = authorization?.GetPolicy(method) != null,
                RepositoryMethod = method.ToString(),
                Uri = uri,
            };
            switch (method)
            {
                case RepositoryMethods.Exist:
                    MapExists<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Insert:
                    MapInsert<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Update:
                    MapUpdate<T, TKey>(api, request);
                    break;
            }
            api.Requests.Add(request);
            return app;
        }
        private static void MapExists<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            request.RequestBody = apiMap.Key;
            request.Response = new State<T, TKey>(true, null, 200, "You may return a possible message and a code.");
        }
        private static void MapInsert<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            request.HttpMethod = HttpMethods.Post;
            var entity = new Entity<T, TKey>((T)apiMap.Model!, (TKey)apiMap.Key!);
            request.RequestBody = entity;
            request.Response = new State<T, TKey>(true, entity, 200, "You may return a possible message and a code.");
        }
        private static void MapUpdate<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            request.HttpMethod = HttpMethods.Post;
            var entity = new Entity<T, TKey>((T)apiMap.Model!, (TKey)apiMap.Key!);
            request.RequestBody = entity;
            request.Response = new State<T, TKey>(true, entity, 200, "You may return a possible message and a code.");
        }
        private static IServiceProvider? serviceProviderForPopulation = null;
        private static T PopulateRandom<T>()
        {
            if (serviceProviderForPopulation == null)
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddPopulationService();
                serviceProviderForPopulation = serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;
            }
            var populationService = serviceProviderForPopulation.GetService<IPopulation<T>>()!;
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
