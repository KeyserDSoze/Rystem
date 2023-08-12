using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EndpointRouteBuilderExtensions
    {
        private static void AddGet<T, TKey, TService>(IEndpointRouteBuilder app,
            RequestApiMap apiMap,
            string name,
            string factoryName,
            TKey? defaultKey,
            T? defaultEntity,
            ApiAuthorization? authorization)
           where TKey : notnull
        {
            if (defaultKey != null && defaultEntity != null)
            {
                apiMap.ResponseWith = "Entity";
                apiMap.Response = defaultEntity;
            }
            app.AddApi<T, TKey, TService>(apiMap, name, factoryName, authorization,
                RepositoryMethods.Get, null,
                async (TKey key, TService service, CancellationToken cancellationToken) =>
                {
                    var response = await Try.WithDefaultOnCatchAsync(() =>
                    {
                        var queryService = service as IQueryPattern<T, TKey>;
                        return queryService!.GetAsync(key, cancellationToken);
                    });
                    if (response.Exception != null)
                        return Results.Problem(response.Exception.Message, string.Empty, StatusCodes.Status500InternalServerError);
                    return Results.Json(response.Entity, RepositoryOptions.JsonSerializerOptions);
                }
            );
        }

        private static void AddExist<T, TKey, TService>(IEndpointRouteBuilder app,
            RequestApiMap apiMap,
            string name,
            string factoryName,
            TKey? defaultKey,
            T? defaultEntity,
            ApiAuthorization? authorization)
           where TKey : notnull
        {
            if (defaultKey != null && defaultEntity != null)
            {
                apiMap.ResponseWith = "State";
                apiMap.Response = new State<T, TKey>(
                            true,
                            new Entity<T, TKey>(defaultEntity, defaultKey)
                        );
            }
            app.AddApi<T, TKey, TService>(apiMap, name, factoryName, authorization,
                RepositoryMethods.Exist, null,
                async (TKey key, TService service, CancellationToken cancellationToken) =>
                {
                    var response = await Try.WithDefaultOnCatchAsync(() =>
                    {
                        var queryService = service as IQueryPattern<T, TKey>;
                        return queryService!.ExistAsync(key, cancellationToken);
                    });
                    if (response.Exception != null)
                        return Results.Problem(response.Exception.Message, string.Empty, StatusCodes.Status500InternalServerError);
                    return Results.Json(response.Entity, RepositoryOptions.JsonSerializerOptions);
                }
            );
        }

        private static void AddQuery<T, TKey, TService>(IEndpointRouteBuilder app,
            RequestApiMap apiMap,
            string name,
            string factoryName,
            TKey? defaultKey,
            T? defaultEntity,
            ApiAuthorization? authorization)
            where TKey : notnull
        {
            if (defaultKey != null && defaultEntity != null)
            {
                apiMap.ResponseWith = "List";
                var response = new List<Entity<T, TKey>>()
                {
                    new Entity<T, TKey>(defaultEntity, defaultKey)
                };
                apiMap.Response = response;
                apiMap.HasStream = true;
            }

            _ = app.MapPost(apiMap.Uri,
                async (HttpRequest request,
                    [FromBody] SerializableFilter? serializableFilter, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await Try.WithDefaultOnCatchAsync(async () =>
                        {
                            var filter = (serializableFilter ?? SerializableFilter.Empty).Deserialize<T>();
                            var queryService = factoryService.Create(factoryName) as IQueryPattern<T, TKey>;
                            return await queryService!.QueryAsync(filter, cancellationToken).ToListAsync().NoContext();
                        });
                        if (response.Exception != null)
                            return Results.Problem(response.Exception.Message, string.Empty, StatusCodes.Status500InternalServerError);
                        return Results.Json(response.Entity, RepositoryOptions.JsonSerializerOptions);
                    }
                    catch (OperationCanceledException)
                    {
                        return Results.StatusCode(409);
                    }
                })
                .WithName($"{nameof(RepositoryMethods.Query)}{factoryName}{name}")
                .WithTags(string.IsNullOrWhiteSpace(factoryName) ? name : $"{name}/factoryName:{factoryName}")
                    .AddAuthorization(authorization, RepositoryMethods.Query, apiMap);

            _ = app.MapPost($"{apiMap.Uri}/Stream", (HttpRequest request,
                    [FromBody] SerializableFilter? serializableFilter, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken)
                =>
                    {
                        return QueryAsStreamAsync(serializableFilter, factoryService.Create(factoryName), cancellationToken);

                        static async IAsyncEnumerable<Entity<T, TKey>> QueryAsStreamAsync(
                            SerializableFilter? serializableFilter, TService service, [EnumeratorCancellation] CancellationToken cancellationToken)
                        {
                            var filter = (serializableFilter ?? SerializableFilter.Empty).Deserialize<T>();
                            var queryService = service as IQueryPattern<T, TKey>;
                            await foreach (var entity in queryService!.QueryAsync(filter, cancellationToken))
                            {
                                yield return entity;
                            }
                        }
                    }
                )
                .WithName($"{nameof(RepositoryMethods.Query)}{factoryName}{name}Stream")
                .WithTags(string.IsNullOrWhiteSpace(factoryName) ? name : $"{name}/factoryName:{factoryName}")
              .AddAuthorization(authorization, RepositoryMethods.Query, apiMap);
        }
        private static void AddOperation<T, TKey, TService>(IEndpointRouteBuilder app,
            RequestApiMap apiMap,
            string name,
            string factoryName,
            TKey? defaultKey,
            T? defaultEntity,
            ApiAuthorization? authorization)
                where TKey : notnull
        {
            if (defaultKey != null && defaultEntity != null)
            {
                apiMap.ResponseWith = "Dynamic";
            }
            _ = app.MapPost(apiMap.Uri,
                async ([FromQuery] string op, [FromQuery] string? returnType,
                    [FromBody] SerializableFilter? serializableFilter, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await Try.WithDefaultOnCatchAsync(async () =>
                        {
                            var filter = (serializableFilter ?? SerializableFilter.Empty).Deserialize<T>();
                            var type = CalculateTypeFromQuery();
                            var queryService = factoryService.Create(factoryName) as IQueryPattern<T, TKey>;
                            var result = await Generics.WithStatic(
                                          typeof(EndpointRouteBuilderExtensions),
                                          nameof(GetResultFromOperation),
                                          typeof(T), typeof(TKey), type)
                                        .InvokeAsync(queryService!, op, filter, cancellationToken)!;
                            return result;
                        });
                        if (response.Exception != null)
                            return Results.Problem(response.Exception.Message, string.Empty, StatusCodes.Status500InternalServerError);
                        return Results.Json(response.Entity, RepositoryOptions.JsonSerializerOptions);
                    }
                    catch (OperationCanceledException)
                    {
                        return Results.StatusCode(409);
                    }
                    Type CalculateTypeFromQuery()
                    {
                        var calculatedType = typeof(object);
                        if (string.IsNullOrWhiteSpace(returnType))
                            return calculatedType;
                        if (PrimitiveMapper.Instance.FromNameToAssemblyQualifiedName.TryGetValue(returnType, out var value))
                            calculatedType = Type.GetType(value);
                        else
                            calculatedType = Type.GetType(returnType);
                        return calculatedType ?? typeof(object);
                    }
                })
                .WithName($"{nameof(RepositoryMethods.Operation)}{factoryName}{name}")
                .WithTags(string.IsNullOrWhiteSpace(factoryName) ? name : $"{name}/factoryName:{factoryName}")
              .AddAuthorization(authorization, RepositoryMethods.Operation, apiMap);
        }
        private static ValueTask<TProperty> GetResultFromOperation<T, TKey, TProperty>(
            IQueryPattern<T, TKey> queryService,
            string operationName,
            IFilterExpression filter,
            CancellationToken cancellationToken)
            where TKey : notnull
            => queryService.OperationAsync(
                new OperationType<TProperty>(operationName),
                filter, cancellationToken);
        private static void AddInsert<T, TKey, TService>(IEndpointRouteBuilder app,
            RequestApiMap apiMap,
            string name,
            string factoryName,
            TKey? defaultKey,
            T? defaultEntity,
            ApiAuthorization? authorization)
            where TKey : notnull
        {
            if (defaultKey != null && defaultEntity != null)
            {
                apiMap.ResponseWith = "State";
                apiMap.Response = new State<T, TKey>(
                            true,
                            new Entity<T, TKey>(defaultEntity, defaultKey)
                        );
            }
            app.AddApi(apiMap, name, factoryName, authorization,
                RepositoryMethods.Insert,
                async (T entity, TKey key, TService service, CancellationToken cancellationToken) =>
                {
                    var response = await Try.WithDefaultOnCatchAsync(() =>
                    {
                        var commandService = service as ICommandPattern<T, TKey>;
                        return commandService!.InsertAsync(key, entity, cancellationToken);
                    });
                    if (response.Exception != null)
                        return Results.Problem(response.Exception.Message, string.Empty, StatusCodes.Status500InternalServerError);
                    return Results.Json(response.Entity, RepositoryOptions.JsonSerializerOptions);
                },
                null
            );
        }

        private static void AddUpdate<T, TKey, TService>(IEndpointRouteBuilder app,
            RequestApiMap apiMap,
            string name,
            string factoryName,
            TKey? defaultKey,
            T? defaultEntity,
            ApiAuthorization? authorization)
            where TKey : notnull
        {
            if (defaultKey != null && defaultEntity != null)
            {
                apiMap.ResponseWith = "State";
                apiMap.Response = new State<T, TKey>(
                            true,
                            new Entity<T, TKey>(defaultEntity, defaultKey)
                        );
            }
            app.AddApi(apiMap, name, factoryName, authorization,
                RepositoryMethods.Update,
                async (T entity, TKey key, TService service, CancellationToken cancellationToken) =>
                {
                    var response = await Try.WithDefaultOnCatchAsync(() =>
                    {
                        var commandService = service as ICommandPattern<T, TKey>;
                        return commandService!.UpdateAsync(key, entity, cancellationToken);
                    });
                    if (response.Exception != null)
                        return Results.Problem(response.Exception.Message, string.Empty, StatusCodes.Status500InternalServerError);
                    return Results.Json(response.Entity, RepositoryOptions.JsonSerializerOptions);
                },
                null
            );
        }

        private static void AddDelete<T, TKey, TService>(IEndpointRouteBuilder app,
            RequestApiMap apiMap,
            string name,
            string factoryName,
            TKey? defaultKey,
            T? defaultEntity,
            ApiAuthorization? authorization)
            where TKey : notnull
        {
            if (defaultKey != null && defaultEntity != null)
            {
                apiMap.ResponseWith = "State";
                apiMap.Response = new State<T, TKey>(
                            true,
                            new Entity<T, TKey>(defaultEntity, defaultKey)
                        );
            }
            app.AddApi<T, TKey, TService>(apiMap, name, factoryName, authorization,
                RepositoryMethods.Delete, null,
                async (TKey key, TService service, CancellationToken cancellationToken) =>
                {
                    var response = await Try.WithDefaultOnCatchAsync(() =>
                    {
                        var commandService = service as ICommandPattern<T, TKey>;
                        return commandService!.DeleteAsync(key, cancellationToken);
                    });
                    if (response.Exception != null)
                        return Results.Problem(response.Exception.Message, string.Empty, StatusCodes.Status500InternalServerError);
                    return Results.Json(response.Entity, RepositoryOptions.JsonSerializerOptions);
                }
            );
        }
        private static void AddBatch<T, TKey, TService>(IEndpointRouteBuilder app,
            RequestApiMap apiMap,
            string name,
            string factoryName,
            TKey? defaultKey,
            T? defaultEntity,
            ApiAuthorization? authorization)
            where TKey : notnull
        {
            if (defaultKey != null && defaultEntity != null)
            {
                apiMap.ResponseWith = "Batch";
                var response = new BatchResults<T, TKey>();
                response.Results.Add(
                    new BatchResult<T, TKey>(
                        CommandType.Insert,
                        defaultKey,
                        new State<T, TKey>(
                            true,
                            new Entity<T, TKey>(defaultEntity, defaultKey)
                        )
                    )
                );
                response.Results.Add(
                    new BatchResult<T, TKey>(
                        CommandType.Update,
                        defaultKey,
                        new State<T, TKey>(
                            true,
                            new Entity<T, TKey>(defaultEntity, defaultKey)
                        )
                    )
                );
                response.Results.Add(
                    new BatchResult<T, TKey>(
                        CommandType.Delete,
                        defaultKey,
                        new State<T, TKey>(
                            true,
                            new Entity<T, TKey>(defaultEntity, defaultKey)
                        )
                    )
                );
                apiMap.Response = response;
            }
            _ = app.MapPost(apiMap.Uri,
                async ([FromBody] BatchOperations<T, TKey> operations, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken) =>
            {
                try
                {
                    var response = await Try.WithDefaultOnCatchAsync(() =>
                    {
                        var commandService = factoryService.Create(factoryName) as ICommandPattern<T, TKey>;
                        return commandService!.BatchAsync(operations, cancellationToken);
                    });
                    if (response.Exception != null)
                        return Results.Problem(response.Exception.Message, string.Empty, StatusCodes.Status500InternalServerError);
                    return Results.Json(response.Entity, RepositoryOptions.JsonSerializerOptions);
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(409);
                }
            })
            .WithName($"{nameof(RepositoryMethods.Batch)}{factoryName}{name}")
            .WithTags(string.IsNullOrWhiteSpace(factoryName) ? name : $"{name}/factoryName:{factoryName}")
            .AddAuthorization(authorization, RepositoryMethods.Batch, apiMap);
        }
        private static void AddApi<T, TKey, TService>(this IEndpointRouteBuilder app,
            RequestApiMap singleApi,
            string name,
            string factoryName,
            ApiAuthorization? authorization,
            RepositoryMethods method,
            Func<T, TKey, TService, CancellationToken, Task<IResult>>? action,
            Func<TKey, TService, CancellationToken, Task<IResult>>? actionWithNoEntity)
            where TKey : notnull
        {
            RouteHandlerBuilder? apiMapped = null;
            if (KeySettings<TKey>.Instance.IsJsonable && actionWithNoEntity != null)
            {
                singleApi.KeyIsJsonable = true;
                singleApi.HttpMethod = "Post";
                apiMapped = app.MapPost(singleApi.Uri,
                ([FromBody] TKey key, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken)
                    => actionWithNoEntity.Invoke(key, factoryService.Create(factoryName), cancellationToken));
            }
            else if (KeySettings<TKey>.Instance.IsJsonable && action != null)
            {
                singleApi.KeyIsJsonable = true;
                singleApi.HttpMethod = "Post";
                apiMapped = app.MapPost(singleApi.Uri,
                ([FromBody] Entity<T, TKey> entity, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken)
                    => action.Invoke(entity.Value!, entity.Key!, factoryService.Create(factoryName), cancellationToken));
            }
            else if (!KeySettings<TKey>.Instance.IsJsonable && action != null)
            {
                singleApi.HttpMethod = "Post";
                apiMapped = app.MapPost(singleApi.Uri,
                ([FromQuery] string key, [FromBody] T entity, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken)
                    => action.Invoke(entity, KeySettings<TKey>.Instance.Parse(key), factoryService.Create(factoryName), cancellationToken));
            }
            else
            {
                singleApi.HttpMethod = "Get";
                apiMapped = app.MapGet(singleApi.Uri,
                    ([FromQuery] string key, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken)
                        => actionWithNoEntity!.Invoke(KeySettings<TKey>.Instance.Parse(key), factoryService.Create(factoryName), cancellationToken));
            }
            _ = apiMapped!
                    .WithName($"{method}{factoryName}{name}")
                    .WithTags(string.IsNullOrWhiteSpace(factoryName) ? name : $"{name}/factoryName:{factoryName}")
                    .AddAuthorization(authorization, method, singleApi);

        }
        private static RouteHandlerBuilder AddAuthorization(this RouteHandlerBuilder router,
            ApiAuthorization? authorization,
            RepositoryMethods path,
            RequestApiMap map)
        {
            var policies = authorization?.GetPolicy(path);
            if (policies != null)
            {
                map.Policies.AddRange(policies);
                router.RequireAuthorization(policies);
            }
            return router;
        }
    }
}
