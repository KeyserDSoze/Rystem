using System.Linq.Dynamic.Core;
using System.Population.Random;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RepositoryFramework;
using RepositoryFramework.Abstractions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EndpointRouteBuilderExtensions
    {
        private static IEndpointRouteBuilder AddMap(this IEndpointRouteBuilder app,
            string uri,
            Type model,
            Type key,
            RepositoryMethods method,
            string currentName,
            RepositoryFrameworkService service,
            ApiAuthorization? authorization,
            List<string> furtherPolicies)
        {
            _ = typeof(EndpointRouteBuilderExtensions).GetMethod(nameof(AddMapAsGenerics),
                    BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(model, key)
                        .Invoke(null, new object[] { app, uri, method, currentName, service, authorization!, furtherPolicies });
            return app;
        }
        private static IEndpointRouteBuilder AddMapAsGenerics<T, TKey>(this IEndpointRouteBuilder app,
            string uri,
            RepositoryMethods method,
            string currentName,
            RepositoryFrameworkService service,
            ApiAuthorization? authorization,
            List<string> furtherPolicies)
            where TKey : notnull
        {
            var api = EndpointRouteMap.ApiMap.Apis.FirstOrDefault(x => x.Name == currentName && x.FactoryName == service.FactoryName);
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
                EndpointRouteMap.ApiMap.Apis.Add(api);
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
                    var keyResponse = Try.WithDefaultOnCatch(() => PopulateRandom<TKey>());
                    api.Key = keyResponse.Exception == null ? keyResponse.Entity : default;
                    var modelResponse = Try.WithDefaultOnCatch(() => PopulateRandom<T>());
                    api.Model = modelResponse.Exception == null ? modelResponse.Entity : default;
                }
            }
            List<string> policies = new();
            policies.AddRange(furtherPolicies);
            var authorizationPolicies = authorization?.GetPolicy(method);
            if (authorizationPolicies != null)
                policies.AddRange(authorizationPolicies);
            var request = new RequestApiMap
            {
                IsAuthenticated = policies.Any() || authorizationPolicies != null,
                Policies = policies.ToArray(),
                IsAuthorized = policies.Any(),
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
                    MapInsertOrUpdate<T, TKey>(api, request, true);
                    break;
                case RepositoryMethods.Update:
                    MapInsertOrUpdate<T, TKey>(api, request, false);
                    break;
                case RepositoryMethods.Delete:
                    MapDelete<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Batch:
                    MapBatch<T, TKey>(api, request);
                    break;
                case RepositoryMethods.Bootstrap:
                    MatBoostrap<T, TKey>(api, request);
                    break;
            }
            api.Requests.Add(request);
            return app;
        }
        private static void MatBoostrap<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            request.Description = $"Bootstrap a {typeof(T).Name} entity.";
            request.Sample.Response = true;
        }
        private static void MapExists<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            request.Description = $"Retrieve information if a {typeof(T).Name} exists based on a key {typeof(TKey).Name}.";
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
            request.Description = $"Retrieve a {typeof(T).Name} entity based on a key {typeof(TKey).Name}.";
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
            request.Description = $"Make an operation like Count, Average, Maximum, Minimum or Sum of a series of {typeof(T).Name} entities based on several filters.";
            var filter = new SerializableFilter();
            var firstProperty = apiMap.Model!.GetType().GetProperties().FirstOrDefault(x => x.PropertyType.IsPrimitive());
            if (firstProperty != null)
            {
                var value = firstProperty.GetValue(apiMap.Model!, null);
                filter.Operations.Add(new FilterOperationAsString(
                    FilterOperations.Where,
                    FilterRequest.Entity,
                    $"x => x.{firstProperty.Name} == {(firstProperty.PropertyType.IsNumeric() ? value.ToString() : $"\"{value}\"")}"));
            }
            request.Sample.RequestQuery = new Dictionary<string, string>
            {
                { "op" , "Count" },
                { "returnType", GetPrimitiveNameOrAssemblyQualifiedName() ?? string.Empty }
            };
            request.Sample.RequestBody = filter;

            request.Sample.Response = 1;

            string? GetPrimitiveNameOrAssemblyQualifiedName()
            {
                var name = firstProperty?.PropertyType.AssemblyQualifiedName;
                if (name == null)
                    return null;
                if (PrimitiveMapper.Instance.FromAssemblyQualifiedNameToName.TryGetValue(name, out var value))
                    return value;
                return name;
            }
        }
        private static void MapQuery<T, TKey>(ApiMap apiMap, RequestApiMap request)
          where TKey : notnull
        {
            request.Description = $"Retrieve a series of {typeof(T).Name} entities based on several filters. In body request 'o' stands for operations and you may choose between 'q' filters: 1. Select, 2. Where, 4. Top, 8. Skip, 16. OrderBy, 32. OrderByDescending, 64. ThenBy, 128. ThenByDescending, 256. GroupBy; Operation order is important, for instance you can OrderBy something and only then you may use ThenBy for another field to improve your ordering.";
            request.StreamUri = $"{request.Uri}/Stream";
            var entity = new Entity<T, TKey>((T)apiMap.Model!, (TKey)apiMap.Key!);
            var filter = new SerializableFilter();
            var firstProperty = apiMap.Model!.GetType().GetProperties().FirstOrDefault(x => x.PropertyType.IsPrimitive());
            if (firstProperty != null)
            {
                var value = firstProperty.GetValue(apiMap.Model!, null);
                var query = $"x.{firstProperty.Name} == {(firstProperty.PropertyType.IsNumeric() ? value!.ToString() : $"\"{value}\"")}";
                var queryForLesser = $"x.{firstProperty.Name} <= {(firstProperty.PropertyType.IsNumeric() ? value!.ToString() + "1" : $"\"{value}1\"")}";
                var queryForLesser2 = $"x.{firstProperty.Name} <= {(firstProperty.PropertyType.IsNumeric() ? value!.ToString() + "2" : $"\"{value}2\"")}";
                filter.Operations.Add(new FilterOperationAsString(
                    FilterOperations.Where,
                    FilterRequest.Entity,
                    $"x => {query} && ({queryForLesser} || {queryForLesser2})"));
                filter.Operations.Add(new FilterOperationAsString(
                    FilterOperations.OrderBy,
                    FilterRequest.Entity,
                    $"x => x.{firstProperty.Name}"));
                var secondProperty = apiMap.Model!.GetType().GetProperties().Where(x => x.PropertyType.IsPrimitive()).Skip(1).FirstOrDefault();
                if (secondProperty != null)
                {
                    filter.Operations.Add(new FilterOperationAsString(
                        FilterOperations.ThenBy,
                        FilterRequest.Entity,
                        $"x => x.{secondProperty.Name}"));
                }
            }
            request.Sample.RequestBody = filter;
            request.Sample.Response = new List<Entity<T, TKey>>() { entity };
        }
        private static void MapInsertOrUpdate<T, TKey>(ApiMap apiMap, RequestApiMap request, bool isInsert)
            where TKey : notnull
        {
            request.Description = $"{(isInsert ? "Insert" : "Update")} a {typeof(T).Name} entity based on a key {typeof(TKey).Name}";
            var entity = new Entity<T, TKey>((T)apiMap.Model!, (TKey)apiMap.Key!);
            request.Sample.RequestBody = entity;
            request.Sample.Response = new State<T, TKey>(true, entity, 200, "You may return a possible message and a code.");
        }
        private static void MapDelete<T, TKey>(ApiMap apiMap, RequestApiMap request)
            where TKey : notnull
        {
            request.Description = $"Delete a {typeof(T).Name} entity based on a key {typeof(TKey).Name}.";
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
            request.Description = $"Make a series of operations: Insert, Update and/or Delete for any type of {typeof(T).Name} entities, each entity based on a key {typeof(TKey).Name}.";
            request.StreamUri = $"{request.Uri}/Stream";
            var key = (TKey)apiMap.Key!;
            var entity = new Entity<T, TKey>((T)apiMap.Model!, key);
            var operations = new BatchOperations<T, TKey>();
            operations.AddInsert(key, entity.Value!);
            operations.AddUpdate(key, entity.Value!);
            operations.AddDelete(key);
            var response = new List<BatchResult<T, TKey>>
            {
                new BatchResult<T, TKey>(
                    CommandType.Insert,
                    key,
                    new State<T, TKey>(
                        true,
                        entity,
                        200,
                        "You may return a possible message and a code."
                    )
                ),
                new BatchResult<T, TKey>(
                    CommandType.Update,
                    key,
                    new State<T, TKey>(
                        true,
                        entity,
                        200,
                        "You may return a possible message and a code."
                    )
                ),
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
            };
            request.Sample.RequestBody = operations;
            request.Sample.Response = response;
        }
        private static IServiceProvider? s_serviceProviderForPopulation = null;
        private static T PopulateRandom<T>()
        {
            if (s_serviceProviderForPopulation == null)
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddPopulationService();
                s_serviceProviderForPopulation = serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;
            }
            var populationService = s_serviceProviderForPopulation.GetService<IPopulation<T>>()!;
            return populationService.Populate(1, 1)[0];
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
                        .MapGet("Repository/Map/All", () =>
                        {
                            s_mapAsJson ??= EndpointRouteMap.ApiMap.ToJson();
                            return Results.Text(s_mapAsJson, contentType: "application/json");
                        })
                        .WithTags("_RepositoryMap");
                });
            }
        }
    }
}
