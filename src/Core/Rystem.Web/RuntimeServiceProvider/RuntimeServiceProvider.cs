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
        public static async ValueTask ReBuildAsync(this IServiceCollection services, bool preserveValueForSingletonServices = true)
        {
            if (_services == null)
                throw new ArgumentException($"Please setup in Dependency injection the {nameof(AddRuntimeServiceProvider)} method.");
            var oldServiceProvider = _serviceProvider;
            if (preserveValueForSingletonServices)
            {
                foreach (var service in _services.Where(x => x.Lifetime == ServiceLifetime.Singleton && x.IsKeyedService))
                {
                    var value = _oldServiceProvider?.GetKeyedServices(service.ServiceType, service.ServiceKey);
                    if (value != null)
                    {
                        var entity = value.FirstOrDefault();
                        if (entity != null)
                        {
                            s_implementationInstanceField.SetValue(service, value);
                        }
                    }
                }
                foreach (var service in _services.Where(x => x.Lifetime == ServiceLifetime.Singleton && !x.IsKeyedService).GroupBy(x => x.ServiceType))
                {

                    if (service.Key.ContainsGenericParameters && service.Key.GenericTypeArguments.Length == 0)
                        continue;
                    var values = _oldServiceProvider?.GetServices(service.Key);
                    var allServices = service.GetEnumerator();
                    if (values != null)
                    {
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
