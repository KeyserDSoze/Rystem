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
                IsAuthenticated = authorization?.GetPolicy(method) != null,
                Policies = authorization?.GetPolicy(method),
                IsAuthorized = authorization?.GetPolicy(method) != null,
                RepositoryMethod = method.ToString(),
                Uri = uri,
                Sample = new()
                {
                    BaseUri = uri
                }
            };
            switch (method)
            {
                case RepositoryMethods.Exist:
                    MapExists<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Get:
                    MapGet<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Query:
                    MapQuery<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Operation:
                    MapOperation<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Insert:
                    MapInsertOrUpdate<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Update:
                    MapInsertOrUpdate<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Delete:
                    MapDelete<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Batch:
                    MapBatch<T, TKey>(api, request);
                    break;
            }
            api.Requests.Add(request);
            return app;
        }
        private static void MapExists<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            if (apiMap.KeyIsJsonable)
            {
                request.Sample.RequestBody = apiMap.Key;
            }
            else
            {
                var key = KeySettings<TKey>.Instance.AsString((TKey)apiMap.Key!);
                request.Sample.RequestQuery = new Dictionary<string, string> { { "key", key } };
            }
            request.Sample.Response = new State<T, TKey>(true, null, 200, "You may return a possible message and a code.");
        }
        private static void MapGet<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            if (apiMap.KeyIsJsonable)
            {
                request.Sample.RequestBody = apiMap.Key;
            }
            else
            {
                var key = KeySettings<TKey>.Instance.AsString((TKey)apiMap.Key!);
                request.Sample.RequestQuery = new Dictionary<string, string> { { "key", key } };
            }
            request.Sample.Response = (T)apiMap.Model!;
        }
        private static void MapOperation<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            var firstProperty = apiMap.Model!.GetType().GetProperties().First(x => x.PropertyType.IsPrimitive());
            var operation = (dynamic)typeof(OperationType<>).MakeGenericType(firstProperty.PropertyType).GetProperty("Count", BindingFlags.Public | BindingFlags.Static);

            request.Sample.RequestQuery = new Dictionary<string, string>
            {
                {"op" , operation.Name},
                {"returnType", GetPrimitiveNameOrAssemblyQualifiedName()}
            };

            string? GetPrimitiveNameOrAssemblyQualifiedName()
            {
                var name = operation.Type.AssemblyQualifiedName;
                if (name == null)
                    return null;
                if (PrimitiveMapper.Instance.FromAssemblyQualifiedNameToName.ContainsKey(name))
                    return PrimitiveMapper.Instance.FromAssemblyQualifiedNameToName[name];
                return name;
            }
        }
        private static void MapQuery<T, TKey>(ApiMap apiMap, RequestApiMap request)
          where TKey : notnull
        {
            var entity = new Entity<T, TKey>((T)apiMap.Model!, (TKey)apiMap.Key!);
            var filter = new SerializableFilter();
            var firstProperty = apiMap.Model!.GetType().GetProperties().First(x => x.PropertyType.IsPrimitive());
            var secondProperty = apiMap.Model!.GetType().GetProperties().Where(x => x.PropertyType.IsPrimitive()).Skip(1).FirstOrDefault();
            var value = firstProperty.GetValue(apiMap.Model!, null);
            filter.Operations.Add(new FilterOperationAsString(
                FilterOperations.Where,
                $"x => x.{firstProperty.Name} == {(firstProperty.PropertyType.IsNumeric() ? value.ToString() : $"\"{value}\"")}"));
            filter.Operations.Add(new FilterOperationAsString(
                FilterOperations.OrderBy,
                $"x => x.{firstProperty.Name}"));
            if (secondProperty != null)
                filter.Operations.Add(new FilterOperationAsString(
                    FilterOperations.ThenBy,
                    $"x => x.{secondProperty.Name}"));
            request.Sample.RequestBody = filter;
            request.Sample.Response = new List<Entity<T, TKey>>() { entity };
        }
        private static void MapInsertOrUpdate<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            var entity = new Entity<T, TKey>((T)apiMap.Model!, (TKey)apiMap.Key!);
            request.Sample.RequestBody = entity;
            request.Sample.Response = new State<T, TKey>(true, entity, 200, "You may return a possible message and a code.");
        }
        private static void MapDelete<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            if (apiMap.KeyIsJsonable)
                request.Sample.RequestBody = apiMap.Key;
            else
            {
                request.Sample.RequestQuery = new Dictionary<string, string> { { "key", KeySettings<TKey>.Instance.AsString((TKey)apiMap.Key!) } };
            }
            var entity = new Entity<T, TKey>((T)apiMap.Model!, (TKey)apiMap.Key!);
            request.Sample.Response = new State<T, TKey>(true, entity, 200, "You may return a possible message and a code.");
        }
        private static void MapBatch<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            var key = (TKey)apiMap.Key!;
            var entity = new Entity<T, TKey>((T)apiMap.Model!, key);
            var operations = new BatchOperations<T, TKey>();
            operations.AddInsert(key, entity.Value!);
            operations.AddUpdate(key, entity.Value!);
            operations.AddDelete(key);
            var response = new BatchResults<T, TKey>();
            response.Results.Add(
                new BatchResult<T, TKey>(
                    CommandType.Insert,
                    key,
                    new State<T, TKey>(
                        true,
                        entity,
                        200,
                        "You may return a possible message and a code."
                    )
                )
            );
            response.Results.Add(
                new BatchResult<T, TKey>(
                    CommandType.Update,
                    key,
                    new State<T, TKey>(
                        true,
                        entity,
                        200,
                        "You may return a possible message and a code."
                    )
                )
            );
            response.Results.Add(
                new BatchResult<T, TKey>(
                    CommandType.Delete,
                    key,
                    new State<T, TKey>(
                        true,
                        entity,
                        200,
                        "You may return a possible message and a code."
                    )
                )
            );
            request.Sample.RequestBody = operations;
            request.Sample.Response = response;
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
