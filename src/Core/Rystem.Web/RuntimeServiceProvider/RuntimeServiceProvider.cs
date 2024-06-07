using Microsoft.AspNetCore.Builder;
using System.Reflection;

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
        public static IServiceCollection GetServiceCollection()
        {
            if (_services == null)
                throw new ArgumentException($"Please setup in Dependency injection the {nameof(AddRuntimeServiceProvider)} method.");
            if (_fieldInfoForReadOnlySetter == null)
                _fieldInfoForReadOnlySetter = _services.GetType().GetField("_isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            _fieldInfoForReadOnlySetter?.SetValue(_services, false);
            return _services;
        }
        public static async ValueTask ReBuildAsync(this IServiceCollection services)
        {
            if (_services == null)
                throw new ArgumentException($"Please setup in Dependency injection the {nameof(AddRuntimeServiceProvider)} method.");
            _serviceProvider = _services.BuildServiceProvider();
            _fieldInfoForReadOnlySetter?.SetValue(_services, true);
            _updater?.Invoke(_serviceProvider);
            await _serviceProvider.WarmUpAsync();
        }
        public static T UseRuntimeServiceProvider<T>(this T webApplication)
            where T : IApplicationBuilder
        {
            if (_webApplication == null)
            {
                _webApplication = webApplication;
                webApplication.Use((context, next) =>
                {
                    if (_serviceProvider != null)
                        context.RequestServices = _serviceProvider.CreateScope().ServiceProvider;
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
