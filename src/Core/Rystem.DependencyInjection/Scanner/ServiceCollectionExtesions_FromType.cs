﻿using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static ScanResult ScanFromType<T, TScanAssemblyRetriever>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            => services.Scan(typeof(T), lifetime, typeof(TScanAssemblyRetriever).Assembly);
        public static ScanResult ScanFromType(
            this IServiceCollection services,
            Type serviceType,
            Type scanAssemblyRetriever,
            ServiceLifetime lifetime)
            => services.Scan(serviceType, lifetime, scanAssemblyRetriever.Assembly);
        public static ScanResult ScanFromType<TScanAssemblyRetriever>(
           this IServiceCollection services,
           ServiceLifetime lifetime)
            => services.Scan(lifetime, typeof(TScanAssemblyRetriever).Assembly);
    }
}
