using System.Linq.Dynamic.Core;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EndpointRouteBuilderExtensions
    {
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
                {
                    _ = app.UseApiFromRepository(service.Key, ApiSettings.Instance, authorization);
                }
                return app;
            });
        /// <summary>
        /// Configures API authorization using services from a repository framework.
        /// </summary>
        /// <typeparam name="TEndpointRouteBuilder">Specifies the type that provides the routing capabilities for the API.</typeparam>
        /// <typeparam name="TEntity">Defines the type of the entity that will be managed by the repository framework.</typeparam>
        /// <typeparam name="TKey">Indicates the type used as the key for identifying entities in the repository.</typeparam>
        /// <param name="app">Represents the application builder used to configure the API endpoints.</param>
        /// <returns>Returns an instance of an API authorization builder for further configuration.</returns>
        public static IApiAuthorizationBuilder UseApiFromRepositoryFramework<TEndpointRouteBuilder, TEntity, TKey>(
          this TEndpointRouteBuilder app,
          string? name = null)
          where TEndpointRouteBuilder : IEndpointRouteBuilder
            where TKey : notnull
              => new ApiAuthorizationBuilder(authorization =>
              {
                  var services = app.ServiceProvider.GetService<RepositoryFrameworkRegistry>();
                  name ??= string.Empty;
                  foreach (var service in services!.Services.Where(x => x.Value.KeyType == typeof(TKey) && x.Value.ModelType == typeof(TEntity) && x.Value.FactoryName == name).GroupBy(x => x.Value.ModelType))
                  {
                      _ = app.UseApiFromRepository(service.Key, ApiSettings.Instance, authorization);
                  }
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
            if (settings.HasModelsApi)
                app
                    .AddApiModels();
            var registry = app.ServiceProvider.GetService<RepositoryFrameworkRegistry>();
            var services = registry!.GetByModel(modelType);
            var currentName = modelType.Name;
            if (settings.Names.ContainsKey(modelType.FullName!))
                currentName = settings.Names[modelType.FullName!];
            else if (modelType.IsGenericType)
                currentName = $"{modelType.Name}{string.Join('_', modelType.GetGenericArguments().Select(x => x.Name))}";
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
                if (service.ExposedMethods != RepositoryMethods.None)
                {
                    if (s_setupRepositories.ContainsKey(service.Key))
                        continue;
                    Dictionary<string, bool> configuredMethods = new();
                    s_setupRepositories.Add(service.Key, true);
                    foreach (var method in service.ImplementationType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var currentMethod = s_possibleMethods[service.Type].FirstOrDefault(x => x.Name == $"Add{method.Name.Replace("Async", string.Empty)}");
                        if (currentMethod != null && !configuredMethods.ContainsKey(currentMethod.Name))
                        {
                            if (!service.ExposedMethods.HasFlag(currentMethod.Method))
                                continue;
                            if (!service.ImplementationType.HasInterface<IDefaultIntegration>())
                            {
                                var isNotImplemented = false;
                                Try.WithDefaultOnCatch(() =>
                                {
                                    var instructions = method.GetBodyAsString();
                                    isNotImplemented = instructions.Contains(NotImplementedExceptionIlOperation);
                                });
                                if (isNotImplemented)
                                    continue;
                            }
                            var uri = $"{settings.StartingPath}/{currentName}/{(string.IsNullOrWhiteSpace(service.FactoryName) ? string.Empty : $"{service.FactoryName}/")}{currentMethod.Method}";
                            _ = typeof(EndpointRouteBuilderExtensions).GetMethod(currentMethod.Name, BindingFlags.NonPublic | BindingFlags.Static)!
                               .MakeGenericMethod(modelType, service.KeyType, service.InterfaceType)
                               .Invoke(null, new object[] { app, uri, currentName, service.FactoryName, authorization!, service.Policies });
                            configuredMethods.Add(currentMethod.Name, true);
                            if (settings.HasMapApi)
                                app.AddMap(uri, modelType, service.KeyType, currentMethod.Method, currentName, service, authorization, service.Policies);
                        }
                    }
                }
            }
            return app;
        }
    }
}
