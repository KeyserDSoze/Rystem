using Microsoft.AspNetCore.Builder;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;

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
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> s_cancellationTokenSources = new();
        private static int s_rebuildTimes = 0;
        public static async ValueTask<IServiceProvider> ReBuildAsync(this IServiceCollection services, bool preserveValueForSingletonServices = true)
        {
            if (_services == null)
                throw new ArgumentException($"Please setup in Dependency injection the {nameof(AddRuntimeServiceProvider)} method.");
            Interlocked.Increment(ref s_rebuildTimes);
            var rebuildId = Guid.NewGuid().ToString();
            var tokens = new List<CancellationTokenSource>();
            foreach (var token in s_cancellationTokenSources)
            {
                if (token.Value.IsCancellationRequested)
                    continue;
                tokens.Add(token.Value);
            }
            foreach (var token in tokens)
            {
                if (!token.IsCancellationRequested)
                {
                    await token.CancelAsync();
                    token.Dispose();
                }
            }
            var tokenSource = new CancellationTokenSource();
            s_cancellationTokenSources.TryAdd(rebuildId, tokenSource);
            var oldServiceProvider = _serviceProvider;
            if (preserveValueForSingletonServices)
                _services.PreserveValueForSingleton(tokenSource.Token);
            if (!tokenSource.Token.IsCancellationRequested)
                _oldServiceProvider = oldServiceProvider;
            if (!tokenSource.Token.IsCancellationRequested)
                _serviceProvider = _services.BuildServiceProvider();
            if (!tokenSource.Token.IsCancellationRequested)
                _updater?.Invoke(_serviceProvider!);
            Interlocked.Decrement(ref s_rebuildTimes);
            while (s_rebuildTimes > 0)
            {
                await Task.Delay(100);
            }
            s_cancellationTokenSources.TryRemove(rebuildId, out var disposableToken);
            if (disposableToken?.IsCancellationRequested == false)
            {
                await disposableToken.CancelAsync();
                disposableToken.Dispose();
            }
            _fieldInfoForReadOnlySetter?.SetValue(_services, true);
            return _serviceProvider!;
        }
        private static void PreserveValueForSingleton(this IServiceCollection services, CancellationToken cancellationToken)
        {
            foreach (var serviceByTypeAndByKey in services.Where(x => x.Lifetime == ServiceLifetime.Singleton).GroupBy(x => (x.ServiceType, x.IsKeyedService ? x.ServiceKey : null)))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                if (serviceByTypeAndByKey.Key.ServiceType.ContainsGenericParameters && serviceByTypeAndByKey.Key.ServiceType.GenericTypeArguments.Length == 0)
                    continue;
                var values = serviceByTypeAndByKey.Key.Item2 == null ? _oldServiceProvider?.GetServices(serviceByTypeAndByKey.Key.ServiceType) :
                    _oldServiceProvider?.GetKeyedServices(serviceByTypeAndByKey.Key.ServiceType, serviceByTypeAndByKey.Key.Item2);
                if (values?.IsEmpty() == false)
                {
                    var allServices = serviceByTypeAndByKey.GetEnumerator();
                    foreach (var value in values)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
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
