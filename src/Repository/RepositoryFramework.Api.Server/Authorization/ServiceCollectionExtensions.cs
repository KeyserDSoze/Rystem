using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using RepositoryFramework;
using RepositoryFramework.Api.Server.Authorization;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add all authorization classes to your repository or CQRS pattern.
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection ScanAuthorizationForRepositoryFramework(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient,
            params Assembly[] assemblies)
        {
            var registry = services
                .TryAddSingletonAndGetService<RepositoryFrameworkRegistry>();
            foreach (var groupedService in registry.Services.Select(x => x.Value).GroupBy(x => new { x.ModelType, x.KeyType }))
            {
                var genericArguments = groupedService.First().InterfaceType.GetGenericArguments();
                var type = typeof(IRepositoryAuthorization<,>);
                var serviceType = type.MakeGenericType(genericArguments);
                var addedFromScan = services.Scan(serviceType, lifetime, assemblies);
                if (addedFromScan.Count > 0)
                {
                    if (!services.Any(x => !x.IsKeyedService && x.ImplementationType == typeof(RepositoryRequirementHandler)))
                        services.AddTransient<IAuthorizationHandler, RepositoryRequirementHandler>();
                    foreach (var added in addedFromScan.Implementations)
                    {
                        var policyName = $"{added.FullName}_{groupedService.Key.KeyType.FullName}_{groupedService.Key.ModelType.FullName}";
                        foreach (var service in groupedService)
                            service.Policies.Add(policyName);
                        var method = typeof(RepositoryPolicyBuilder<,>).MakeGenericType(genericArguments).GetMethod("ReadKeyValue", BindingFlags.Static | BindingFlags.NonPublic);
                        services.AddAuthorization(o =>
                        {
                            o.AddPolicy(policyName,
                                p => p.AddRequirements(
                                    new RepositoryRequirement(
                                        policyName,
                                        serviceType,
                                        groupedService.Key.KeyType,
                                        groupedService.Key.ModelType,
                                        added,
                                        (httpContextAccessor) => method.Invoke(null, new object[1] { httpContextAccessor }) as Task<RepositoryRequirementReader>)));
                        });
                    }
                }
            }
            return services;
        }
        /// <summary>
        /// Add all authorization classes to your repository or CQRS pattern from current domain.
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection ScanAuthorizationForRepositoryFramework(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
            => services.ScanAuthorizationForRepositoryFramework(lifetime, AppDomain.CurrentDomain.GetAssemblies());
    }
}
