using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
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
            foreach (var service in registry.Services.Select(x => x.Value))
            {
                var genericArguments = service.InterfaceType.GetGenericArguments();
                var type = typeof(IRepositoryAuthorization<,>);
                var serviceType = type.MakeGenericType(genericArguments);
                var addedFromScan = services.Scan(serviceType, lifetime, assemblies);
                if (addedFromScan.Count > 0)
                {
                    if (!services.Any(x => x.ImplementationType == typeof(RepositoryRequirementHandler)))
                        services.AddTransient<IAuthorizationHandler, RepositoryRequirementHandler>();
                    foreach (var added in addedFromScan.Implementations)
                    {
                        var policyName = $"{added.FullName}_{service.KeyType.FullName}_{service.ModelType.FullName}";
                        service.Policies.Add(policyName);
                        var method = typeof(RepositoryPolicyBuilder<,>).MakeGenericType(genericArguments).GetMethod("ReadKeyValue", BindingFlags.Static | BindingFlags.NonPublic);
                        services.AddAuthorization(o =>
                        {
                            o.AddPolicy(policyName,
                                p => p.AddRequirements(
                                    new RepositoryRequirement(
                                        policyName,
                                        type,
                                        service.KeyType,
                                        service.ModelType,
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
