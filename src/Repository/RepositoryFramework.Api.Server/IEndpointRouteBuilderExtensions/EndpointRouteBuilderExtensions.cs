using System.Linq.Dynamic.Core;
using System.Net;
using System.Population.Random;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EndpointRouteBuilderExtensions
    {
        private static readonly Dictionary<string, bool> s_setupRepositories = new();
        private static readonly ApisMap s_map = new();
        /// <summary>
        /// Add repository or CQRS service injected as api for your <typeparamref name="T"/> model.
        /// </summary>
        /// <typeparam name="T">Model of your repository or CQRS that you want to add as api</typeparam>
        /// <param name="app">IEndpointRouteBuilder</param>
        /// <returns>ApiAuthorizationBuilder</returns>
        public static IApiAuthorizationBuilder UseApiFromRepository<T>(this IEndpointRouteBuilder app)
            => new ApiAuthorizationBuilder(authorization => app.UseApiFromRepository(typeof(T), ApiSettings.Instance, authorization));

        /// <summary>
        /// Add all repository or CQRS services injected as api.
        /// </summary>
        /// <typeparam name="TEndpointRouteBuilder"></typeparam>
        /// <param name="app">IEndpointRouteBuilder</param>
        /// <param name="startingPath">By default is "api", but you can choose your path. https://{your domain}/{startingPath}</param>
        /// <returns>ApiAuthorizationBuilder</returns>
        public static IApiAuthorizationBuilder UseApiFromRepositoryFramework<TEndpointRouteBuilder>(
            this TEndpointRouteBuilder app)
            where TEndpointRouteBuilder : IEndpointRouteBuilder
        => new ApiAuthorizationBuilder(authorization =>
            {
                var services = app.ServiceProvider.GetService<RepositoryFrameworkRegistry>();
                foreach (var service in services!.Services.GroupBy(x => x.Value.ModelType))
                    _ = app.UseApiFromRepository(service.Key, ApiSettings.Instance, authorization);
                return app;
            });

        private const string NotImplementedExceptionIlOperation = "newobj instance void System.NotImplementedException";
        private sealed class RepositoryMethodValue
        {
            public string Name { get; init; }
            public RepositoryMethods Method { get; init; }
            public string DefaultHttpMethod { get; init; }
        }
        private static readonly Dictionary<PatternType, List<RepositoryMethodValue>> s_possibleMethods = new()
        {
            {
                PatternType.Repository,
                new() {
                    new()
                    {
                       Name = nameof(AddGet),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Get
                    },
                    new()
                    {
                       Name = nameof(AddQuery),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Query
                    },
                    new()
                    {
                       Name = nameof(AddExist),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Exist
                    },
                    new()
                    {
                       Name = nameof(AddOperation),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Operation
                    },
                    new()
                    {
                       Name = nameof(AddInsert),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Insert
                    },
                    new()
                    {
                       Name = nameof(AddUpdate),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Update
                    },
                    new()
                    {
                       Name = nameof(AddDelete),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Delete
                    },
                    new()
                    {
                       Name = nameof(AddBatch),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Batch
                    },
                }
            },
            {
                PatternType.Query,
                new() {
                    new()
                    {
                       Name = nameof(AddGet),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Get
                    },
                    new()
                    {
                       Name = nameof(AddQuery),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Query
                    },
                    new()
                    {
                       Name = nameof(AddExist),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Exist
                    },
                    new()
                    {
                       Name = nameof(AddOperation),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Operation
                    },
                }
            },
            {
                PatternType.Command,
                new() {
                     new()
                    {
                       Name = nameof(AddInsert),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Insert
                    },
                    new()
                    {
                       Name = nameof(AddUpdate),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Update
                    },
                    new()
                    {
                       Name = nameof(AddDelete),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Delete
                    },
                    new()
                    {
                       Name = nameof(AddBatch),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Batch
                    },
                }
            }
        };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "I need reflection in this point to allow the creation of T methods at runtime.")]
        private static TEndpointRouteBuilder UseApiFromRepository<TEndpointRouteBuilder>(
            this TEndpointRouteBuilder app,
            Type modelType,
            ApiSettings settings,
            ApiAuthorization? authorization)
            where TEndpointRouteBuilder : IEndpointRouteBuilder
        {
            if (settings.HasMapApi)
                app
                    .AddApiMap();
            if (s_setupRepositories.ContainsKey(modelType.FullName!))
                return app;
            s_setupRepositories.Add(modelType.FullName!, true);
            var registry = app.ServiceProvider.GetService<RepositoryFrameworkRegistry>();
            var services = registry!.GetByModel(modelType);
            if (!services.Any())
                throw new ArgumentException($"Please check if your {modelType.Name} model has a service injected for IRepository, IQuery, ICommand.");
            var currentName = modelType.Name;
            if (settings.Names.ContainsKey(modelType.FullName!))
                currentName = settings.Names[modelType.FullName!];
            if (app is IApplicationBuilder applicationBuilder)
            {
                if (ApiSettings.Instance.HasSwagger && !ApiSettings.Instance.SwaggerInstalled)
                {
                    applicationBuilder.UseSwaggerUiForRepository(ApiSettings.Instance);
                    ApiSettings.Instance.SwaggerInstalled = true;
                }
                if (ApiSettings.Instance.HasDefaultCors && !ApiSettings.Instance.CorsInstalled)
                {
                    applicationBuilder.UseCors(ApiSettings.AllowSpecificOrigins);
                    ApiSettings.Instance.CorsInstalled = true;
                }
            }
            Dictionary<string, bool> configuredMethods = new();
            foreach (var service in services)
            {
                if (!service.IsNotExposable)
                {
                    var apiServiceName = $"{currentName}-{service.KeyType.Name}";
                    object? defaultKey = null;
                    object? defaultValue = null;
                    if (settings.HasMapApi && !s_map.Apis.Any(x => x.Name == apiServiceName))
                    {
                        defaultKey = app.ServiceProvider.PopulateRandomObject(service.KeyType);
                        defaultValue = app.ServiceProvider.PopulateRandomObject(modelType);
                        s_map.Apis.Add(new()
                        {
                            Name = apiServiceName,
                            Key = defaultKey,
                            Model = defaultValue,
                            PatternType = service.Type.ToString(),
                        });
                    }
                    foreach (var method in service.ImplementationType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var currentMethod = s_possibleMethods[service.Type].FirstOrDefault(x => x.Name == $"Add{method.Name.Replace("Async", string.Empty)}");
                        if (currentMethod != null && !configuredMethods.ContainsKey(currentMethod.Name))
                        {
                            var isNotImplemented = false;
                            Try.WithDefaultOnCatch(() =>
                            {
                                var instructions = method.GetBodyAsString();
                                isNotImplemented = instructions.Contains(NotImplementedExceptionIlOperation);
                            });
                            if (isNotImplemented)
                                continue;

                            var singleApi = new RequestApiMap()
                            {
                                IsAuthenticated = authorization != null,
                                IsAuthorized = authorization != null,
                                Uri = $"{settings.StartingPath}/{currentName}/{currentMethod.Method}",
                                KeyIsJsonable = false,
                                HttpMethod = currentMethod.DefaultHttpMethod,
                                RepositoryMethod = currentMethod.Method.ToString()
                            };

                            _ = typeof(EndpointRouteBuilderExtensions).GetMethod(currentMethod.Name, BindingFlags.NonPublic | BindingFlags.Static)!
                               .MakeGenericMethod(modelType, service.KeyType, service.InterfaceType)
                               .Invoke(null, new object[] { app, singleApi, currentName, defaultKey, defaultValue, authorization! });
                            configuredMethods.Add(currentMethod.Name, true);

                            if (settings.HasMapApi)
                                s_map.Apis.First(x => x.Name == apiServiceName).Requests.Add(singleApi);
                        }
                    }
                }
            }
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
        private static void AddGet<T, TKey, TService>(IEndpointRouteBuilder app,
            RequestApiMap apiMap,
            string name,
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
            app.AddApi<T, TKey, TService>(apiMap, name, authorization,
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
            app.AddApi<T, TKey, TService>(apiMap, name, authorization,
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
                    [FromBody] SerializableFilter? serializableFilter, [FromServices] TService service, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await Try.WithDefaultOnCatchAsync(async () =>
                        {
                            var filter = (serializableFilter ?? SerializableFilter.Empty).Deserialize<T>();
                            var queryService = service as IQueryPattern<T, TKey>;
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
                .WithName($"{nameof(RepositoryMethods.Query)}{name}")
                .WithTags(name)
              .AddAuthorization(authorization, RepositoryMethods.Query, apiMap);

            _ = app.MapPost($"{apiMap.Uri}/Stream", (HttpRequest request,
                    [FromBody] SerializableFilter? serializableFilter, [FromServices] TService service, CancellationToken cancellationToken)
                =>
                    {
                        return QueryAsStreamAsync(serializableFilter, service, cancellationToken);

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
                .WithName($"{nameof(RepositoryMethods.Query)}{name}Stream")
                .WithTags(name)
              .AddAuthorization(authorization, RepositoryMethods.Query, apiMap);
        }
        private static void AddOperation<T, TKey, TService>(IEndpointRouteBuilder app,
            RequestApiMap apiMap,
            string name,
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
                    [FromBody] SerializableFilter? serializableFilter, [FromServices] TService service, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await Try.WithDefaultOnCatchAsync(async () =>
                        {
                            var filter = (serializableFilter ?? SerializableFilter.Empty).Deserialize<T>();
                            var type = CalculateTypeFromQuery();
                            var queryService = service as IQueryPattern<T, TKey>;
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
                .WithName($"{nameof(RepositoryMethods.Operation)}{name}")
                .WithTags(name)
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
            app.AddApi(apiMap, name, authorization,
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
            app.AddApi(apiMap, name, authorization,
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
            app.AddApi<T, TKey, TService>(apiMap, name, authorization,
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
                async ([FromBody] BatchOperations<T, TKey> operations, [FromServices] TService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var response = await Try.WithDefaultOnCatchAsync(() =>
                    {
                        var commandService = service as ICommandPattern<T, TKey>;
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
            .WithName($"{nameof(RepositoryMethods.Batch)}{name}")
            .WithTags(name)
            .AddAuthorization(authorization, RepositoryMethods.Batch, apiMap);
        }
        private static void AddApi<T, TKey, TService>(this IEndpointRouteBuilder app,
            RequestApiMap singleApi,
            string name,
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
                ([FromBody] TKey key, [FromServices] TService service, CancellationToken cancellationToken)
                    => actionWithNoEntity.Invoke(key, service, cancellationToken));
            }
            else if (KeySettings<TKey>.Instance.IsJsonable && action != null)
            {
                singleApi.KeyIsJsonable = true;
                singleApi.HttpMethod = "Post";
                apiMapped = app.MapPost(singleApi.Uri,
                ([FromBody] Entity<T, TKey> entity, [FromServices] TService service, CancellationToken cancellationToken)
                    => action.Invoke(entity.Value!, entity.Key!, service, cancellationToken));
            }
            else if (!KeySettings<TKey>.Instance.IsJsonable && action != null)
            {
                singleApi.HttpMethod = "Post";
                apiMapped = app.MapPost(singleApi.Uri,
                ([FromQuery] string key, [FromBody] T entity, [FromServices] TService service, CancellationToken cancellationToken)
                    => action.Invoke(entity, KeySettings<TKey>.Instance.Parse(key), service, cancellationToken));
            }
            else
            {
                singleApi.HttpMethod = "Get";
                apiMapped = app.MapGet(singleApi.Uri,
                    ([FromQuery] string key, [FromServices] TService service, CancellationToken cancellationToken)
                        => actionWithNoEntity!.Invoke(KeySettings<TKey>.Instance.Parse(key), service, cancellationToken));
            }
            _ = apiMapped!
                    .WithName($"{method}{name}")
                    .WithTags(name)
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
