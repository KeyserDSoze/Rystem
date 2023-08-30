using System.Reflection;
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
            string uri,
            string name,
            string factoryName,
            ApiAuthorization? authorization,
            List<string> furtherPolicies)
           where TKey : notnull
        {
            app.AddApi<T, TKey, TService>(uri, name, factoryName, authorization, furtherPolicies,
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
            string uri,
            string name,
            string factoryName,
            ApiAuthorization? authorization,
            List<string> furtherPolicies)
           where TKey : notnull
        {
            app.AddApi<T, TKey, TService>(uri, name, factoryName, authorization, furtherPolicies,
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
            string uri,
            string name,
            string factoryName,
            ApiAuthorization? authorization,
            List<string> furtherPolicies)
            where TKey : notnull
        {
            _ = app.MapPost(uri,
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
                    .AddAuthorization(authorization, furtherPolicies, RepositoryMethods.Query);

            _ = app.MapPost($"{uri}/Stream", (HttpRequest request,
                    [FromBody] SerializableFilter? serializableFilter,
                    [FromServices] IFactory<TService> factoryService,
                    CancellationToken cancellationToken) =>
                    {
                        var filter = (serializableFilter ?? SerializableFilter.Empty).Deserialize<T>();
                        var queryService = factoryService.Create(factoryName) as IQueryPattern<T, TKey>;
                        return queryService!.QueryAsync(filter, cancellationToken);
                    }
                )
                .WithName($"{nameof(RepositoryMethods.Query)}{factoryName}{name}Stream")
                .WithTags(string.IsNullOrWhiteSpace(factoryName) ? name : $"{name}/factoryName:{factoryName}")
              .AddAuthorization(authorization, furtherPolicies, RepositoryMethods.Query);
        }
        private static void AddOperation<T, TKey, TService>(IEndpointRouteBuilder app,
            string uri,
            string name,
            string factoryName,
            ApiAuthorization? authorization,
            List<string> furtherPolicies)
                where TKey : notnull
        {
            _ = app.MapPost(uri,
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
              .AddAuthorization(authorization, furtherPolicies, RepositoryMethods.Operation);
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
            string uri,
            string name,
            string factoryName,
            ApiAuthorization? authorization,
            List<string> furtherPolicies)
            where TKey : notnull
        {
            app.AddApi(uri, name, factoryName, authorization, furtherPolicies,
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
            string uri,
            string name,
            string factoryName,
            ApiAuthorization? authorization,
            List<string> furtherPolicies)
            where TKey : notnull
        {
            app.AddApi(uri, name, factoryName, authorization, furtherPolicies,
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
            string uri,
            string name,
            string factoryName,
            ApiAuthorization? authorization,
            List<string> furtherPolicies)
            where TKey : notnull
        {
            app.AddApi<T, TKey, TService>(uri, name, factoryName, authorization, furtherPolicies,
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
            string uri,
            string name,
            string factoryName,
            ApiAuthorization? authorization,
            List<string> furtherPolicies)
            where TKey : notnull
        {
            _ = app.MapPost(uri,
                async ([FromBody] BatchOperations<T, TKey> operations,
                [FromServices] IFactory<TService> factoryService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var response = await Try.WithDefaultOnCatchAsync(async () =>
                    {
                        var commandService = factoryService.Create(factoryName) as ICommandPattern<T, TKey>;
                        return await commandService!
                            .BatchAsync(operations, cancellationToken)
                            .ToListAsync()
                            .NoContext();
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
            .AddAuthorization(authorization, furtherPolicies, RepositoryMethods.Batch);

            _ = app.MapPost($"{uri}/Stream",
                ([FromBody] BatchOperations<T, TKey> operations,
                [FromServices] IFactory<TService> factoryService,
                CancellationToken cancellationToken) =>
                {
                    var commandService = factoryService.Create(factoryName) as ICommandPattern<T, TKey>;
                    return commandService!.BatchAsync(operations, cancellationToken);
                })
                .WithName($"{nameof(RepositoryMethods.Batch)}{factoryName}{name}Stream")
                .WithTags(string.IsNullOrWhiteSpace(factoryName) ? name : $"{name}/factoryName:{factoryName}")
              .AddAuthorization(authorization, furtherPolicies, RepositoryMethods.Batch);
        }
        private static void AddApi<T, TKey, TService>(this IEndpointRouteBuilder app,
            string uri,
            string name,
            string factoryName,
            ApiAuthorization? authorization,
            List<string> furtherPolicies,
            RepositoryMethods method,
            Func<T, TKey, TService, CancellationToken, Task<IResult>>? action,
            Func<TKey, TService, CancellationToken, Task<IResult>>? actionWithNoEntity)
            where TKey : notnull
        {
            RouteHandlerBuilder? apiMapped = null;
            if (KeySettings<TKey>.Instance.IsJsonable && actionWithNoEntity != null)
            {
                apiMapped = app.MapPost(uri,
                ([FromBody] TKey key, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken)
                    => actionWithNoEntity.Invoke(key, factoryService.Create(factoryName), cancellationToken));
            }
            else if (KeySettings<TKey>.Instance.IsJsonable && action != null)
            {
                apiMapped = app.MapPost(uri,
                ([FromBody] Entity<T, TKey> entity, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken)
                    => action.Invoke(entity.Value!, entity.Key!, factoryService.Create(factoryName), cancellationToken));
            }
            else if (!KeySettings<TKey>.Instance.IsJsonable && action != null)
            {
                apiMapped = app.MapPost(uri,
                ([FromQuery] string key, [FromBody] T entity, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken)
                    => action.Invoke(entity, KeySettings<TKey>.Instance.Parse(key), factoryService.Create(factoryName), cancellationToken));
            }
            else
            {
                apiMapped = app.MapGet(uri,
                    ([FromQuery] string key, [FromServices] IFactory<TService> factoryService, CancellationToken cancellationToken)
                        => actionWithNoEntity!.Invoke(KeySettings<TKey>.Instance.Parse(key), factoryService.Create(factoryName), cancellationToken));
            }
            _ = apiMapped!
                    .WithName($"{method}{factoryName}{name}")
                    .WithTags(string.IsNullOrWhiteSpace(factoryName) ? name : $"{name}/factoryName:{factoryName}")
                    .AddAuthorization(authorization, furtherPolicies, method);

        }
        private static RouteHandlerBuilder AddAuthorization(this RouteHandlerBuilder router,
            ApiAuthorization? authorization,
            List<string> furtherPolicies,
            RepositoryMethods path)
        {
            List<string> policies = new();
            policies.AddRange(furtherPolicies);
            var authorizationPolicies = authorization?.GetPolicy(path);
            if (authorizationPolicies != null)
                policies.AddRange(authorizationPolicies);
            if (policies.Any())
            {
                router.RequireAuthorization(policies.ToArray());
            }
            else if (authorizationPolicies != null)
                router.RequireAuthorization();
            return router;
        }
    }
}
