using System.Linq.Dynamic.Core;
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
            var registry = app.ServiceProvider.GetService<RepositoryFrameworkRegistry>();
            var services = registry!.GetByModel(modelType);
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
            foreach (var service in services)
            {
                if (!service.IsNotExposable)
                {
                    if (s_setupRepositories.ContainsKey(service.Key))
                        continue;
                    Dictionary<string, bool> configuredMethods = new();
                    s_setupRepositories.Add(service.Key, true);
                    object? defaultKey = null;
                    object? defaultValue = null;
                    if (settings.HasMapApi && !s_map.Apis.Any(x => x.FullName == service.Key))
                    {
                        defaultKey = app.ServiceProvider.PopulateRandomObject(service.KeyType);
                        defaultValue = app.ServiceProvider.PopulateRandomObject(modelType);
                        s_map.Apis.Add(new()
                        {
                            FullName = service.Key,
                            FactoryName = service.FactoryName,
                            Name = currentName,
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
                                Uri = $"{settings.StartingPath}/{currentName}/{(string.IsNullOrWhiteSpace(service.FactoryName) ? string.Empty : $"{service.FactoryName}/")}{currentMethod.Method}",
                                KeyIsJsonable = false,
                                HttpMethod = currentMethod.DefaultHttpMethod,
                                RepositoryMethod = currentMethod.Method.ToString()
                            };

                            _ = typeof(EndpointRouteBuilderExtensions).GetMethod(currentMethod.Name, BindingFlags.NonPublic | BindingFlags.Static)!
                               .MakeGenericMethod(modelType, service.KeyType, service.InterfaceType)
                               .Invoke(null, new object[] { app, singleApi, currentName, service.FactoryName, defaultKey!, defaultValue!, authorization! });
                            configuredMethods.Add(currentMethod.Name, true);

                            if (settings.HasMapApi)
                                s_map.Apis.First(x => x.FullName == service.Key).Requests.Add(singleApi);
                        }
                    }
                }
            }
            return app;
        }
    }
}
