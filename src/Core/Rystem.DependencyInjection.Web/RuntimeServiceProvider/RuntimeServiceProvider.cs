using Microsoft.AspNetCore.Builder;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RuntimeServiceProvider
    {
        private static IServiceCollection? _services;
        private static IServiceProvider? _serviceProvider;
        private static IServiceProvider? _oldServiceProvider;
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
            return _serviceProvider ?? _oldServiceProvider ?? throw new ArgumentException($"Please setup in Dependency injection the {nameof(UseRuntimeServiceProvider)} method."); ;
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
        private static readonly FieldInfo s_implementationInstanceField = typeof(ServiceDescriptor).GetField("_implementationInstance", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static async ValueTask<IServiceProvider> ReBuildAsync(this IServiceCollection services, bool preserveValueForSingletonServices = true)
        {
            if (_services == null)
                throw new ArgumentException($"Please setup in Dependency injection the {nameof(AddRuntimeServiceProvider)} method.");
            var oldServiceProvider = _serviceProvider;
            if (preserveValueForSingletonServices)
            {
                foreach (var serviceByTypeAndByKey in _services.Where(x => x.Lifetime == ServiceLifetime.Singleton).GroupBy(x => (x.ServiceType, x.IsKeyedService ? x.ServiceKey : null)))
                {
                    if (serviceByTypeAndByKey.Key.ServiceType.ContainsGenericParameters && serviceByTypeAndByKey.Key.ServiceType.GenericTypeArguments.Length == 0)
                        continue;
                    var values = serviceByTypeAndByKey.Key.Item2 == null ? _oldServiceProvider?.GetServices(serviceByTypeAndByKey.Key.ServiceType) :
                        _oldServiceProvider?.GetKeyedServices(serviceByTypeAndByKey.Key.ServiceType, serviceByTypeAndByKey.Key.Item2);
                    if (values?.IsEmpty() == false)
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
            _serviceProvider = _services.BuildServiceProvider();
            _oldServiceProvider = oldServiceProvider;
            _fieldInfoForReadOnlySetter?.SetValue(_services, true);
            _updater?.Invoke(_serviceProvider);
            await _serviceProvider.WarmUpAsync();
            return _serviceProvider;
        }
        public static T UseRuntimeServiceProvider<T>(this T webApplication, bool disposeOldServiceProvider = true)
            where T : IApplicationBuilder
        {
            if (_webApplication == null)
            {
                _webApplication = webApplication;
                _oldServiceProvider = _webApplication.ApplicationServices;
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
