using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RuntimeServiceProvider
    {
        private static IServiceCollection? _services;
        private static IServiceProvider? _serviceProvider;
        private static IApplicationBuilder? _webApplication;
        private static FieldInfo? _fieldInfoForReadOnlySetter;
        private static Action<IServiceProvider>? _updater;
        public static IServiceCollection AddRuntimeServiceProvider(this IServiceCollection services)
        {
            _services = services;
            return _services;
        }
        public static IServiceProvider GetServiceProvider()
        {
            return _serviceProvider ?? throw new ArgumentException($"Please setup in Dependency injection the {nameof(UseRuntimeServiceProvider)} method."); ;
        }
        public static IServiceCollection GetServiceCollection()
        {
            if (_services == null)
                throw new ArgumentException($"Please setup in Dependency injection the {nameof(AddRuntimeServiceProvider)} method.");
            if (_fieldInfoForReadOnlySetter == null)
                _fieldInfoForReadOnlySetter = _services.GetType().GetField("_isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            _fieldInfoForReadOnlySetter?.SetValue(_services, false);
            return _services;
        }
        private static readonly object s_addingServicesLock = new();
        public static IServiceCollection AddServicesToServiceCollectionWithLock(Action<IServiceCollection> configureFurtherServices)
        {
            GetServiceCollection();
            lock (s_addingServicesLock)
                configureFurtherServices(_services!);
            return _services!;
        }
        private static readonly FieldInfo s_implementationInstanceField = typeof(ServiceDescriptor).GetField("_implementationInstance", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly object s_lock = new();
        private static int s_numberOfServices = 0;
        public static async ValueTask<IServiceProvider> RebuildAsync(this IServiceCollection services, bool preserveValueForSingletonServices = true)
        {
            if (_services == null)
                throw new ArgumentException($"Please setup in Dependency injection the {nameof(AddRuntimeServiceProvider)} method.");
            var numberOfServices = 0;
            lock (s_lock)
            {
                numberOfServices = services.Count;
            }
            var oldServiceProvider = _serviceProvider;
            if (preserveValueForSingletonServices)
            {
                var descriptors = new List<ServiceDescriptor>();
                lock (s_lock)
                {
                    for (var i = 0; i < services.Count; i++)
                    {
                        var descriptor = services[i];
                        if (descriptor != null && descriptor.Lifetime == ServiceLifetime.Singleton)
                        {
                            descriptors.Add(descriptor);
                        }
                    }
                }
                foreach (var serviceByTypeAndByKey in descriptors.GroupBy(x => (x.ServiceType, x.IsKeyedService ? x.ServiceKey : null)))
                {
                    if (serviceByTypeAndByKey.Key.ServiceType.ContainsGenericParameters && serviceByTypeAndByKey.Key.ServiceType.GenericTypeArguments.Length == 0)
                        continue;
                    var values = serviceByTypeAndByKey.Key.Item2 == null ? oldServiceProvider?.GetServices(serviceByTypeAndByKey.Key.ServiceType) :
                        oldServiceProvider?.GetKeyedServices(serviceByTypeAndByKey.Key.ServiceType, serviceByTypeAndByKey.Key.Item2);
                    if (values?.Any() == true)
                    {
                        var allServices = serviceByTypeAndByKey.GetEnumerator();
                        foreach (var value in values)
                        {
                            if (allServices.MoveNext())
                            {
                                var actualService = allServices.Current;
                                if (value != null && actualService != null)
                                    s_implementationInstanceField.SetValue(actualService, value);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            if (numberOfServices > s_numberOfServices)
            {
                var serviceProvider = _services.BuildServiceProvider();
                lock (s_lock)
                {
                    if (numberOfServices > s_numberOfServices)
                    {
                        s_numberOfServices = numberOfServices;
                        _serviceProvider = serviceProvider;
                        _updater?.Invoke(_serviceProvider);
                    }
                }
            }
            await _serviceProvider!.WarmUpAsync().NoContext();
            return _serviceProvider!;
        }
        public static ValueTask<IServiceProvider> RebuildAsync(bool preserveValueForSingletonServices = true)
            => _services!.RebuildAsync(preserveValueForSingletonServices);
        public static T UseRuntimeServiceProvider<T>(this T webApplication, bool disposeOldServiceProvider = true)
            where T : IApplicationBuilder
        {
            if (_webApplication == null)
            {
                _webApplication = webApplication;
                _serviceProvider = _webApplication.ApplicationServices;
                webApplication.Use((context, next) =>
                {
                    if (_serviceProvider != null)
                    {
                        var services = context.RequestServices;
                        context.RequestServices = _serviceProvider.CreateScope().ServiceProvider;
                        if (disposeOldServiceProvider)
                        {
                            try
                            {
                                if (services is IDisposable disposable)
                                    disposable?.Dispose();
                                if (services is IAsyncDisposable asyncDisposable)
                                    _ = asyncDisposable?.DisposeAsync();
                            }
                            catch { }
                        }
                    }
                    return next(context);
                });
                if (_updater == null)
                    _updater = services =>
                    {
                        if (_webApplication != null)
                        {
                            var hostField = _webApplication.GetType().GetField("_host", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (hostField != null)
                            {
                                var host = hostField.GetValue(_webApplication);
                                if (host != null)
                                {
                                    var servicesField = host.GetType().GetField("<Services>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (servicesField != null)
                                    {
                                        servicesField.SetValue(host, services);
                                    }
                                }
                            }
                        }
                    };
            }
            return webApplication;
        }
    }
}
