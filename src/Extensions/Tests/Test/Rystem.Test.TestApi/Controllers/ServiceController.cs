using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.TestApi.Models;
using Rystem.Test.TestApi.Services;

namespace Rystem.Test.TestApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ServiceController : ControllerBase
    {
        private readonly ILogger<ServiceController> _logger;
        private readonly SingletonService _singletonService;
        private readonly Singleton2Service _singleton2Service;
        private readonly ScopedService _scopedService;
        private readonly Scoped2Service _scoped2Service;
        private readonly TransientService _transientService;
        private readonly Transient2Service _transient2Service;
        private readonly IServiceProvider _serviceProvider;

        public ServiceController(ILogger<ServiceController> logger,
                SingletonService singletonService,
                Singleton2Service singleton2Service,
                ScopedService scopedService,
                Scoped2Service scoped2Service,
                TransientService transientService,
                Transient2Service transient2Service,
                IServiceProvider serviceProvider)
        {
            _logger = logger;
            _singletonService = singletonService;
            _singleton2Service = singleton2Service;
            _scopedService = scopedService;
            _scoped2Service = scoped2Service;
            _transientService = transientService;
            _transient2Service = transient2Service;
            _serviceProvider = serviceProvider;
        }
        [HttpGet]
        public async Task<ServiceWrapper> GetAsync()
        {
            var value = _serviceProvider.GetService<AddedService>();
            if (value == null)
            {
                await RuntimeServiceProvider.GetServiceCollection()
                     .AddSingleton<AddedService>()
                     .RebuildAsync();
            }
            return new ServiceWrapper
            {
                TransientService = _transientService,
                Transient2Service = _transient2Service,
                ScopedService = _scopedService,
                Scoped2Service = _scoped2Service,
                SingletonService = _singletonService,
                Singleton2Service = _singleton2Service,
                AddedService = value
            };
        }
        [HttpGet]
        public bool Factory([FromQuery] string? name = null)
        {
            var factory = _serviceProvider.GetRequiredService<IFactory<Factorized>>();
            return factory.Create(name) != null;
        }
        [HttpGet]
        public bool FactoryWithoutRebuild()
        {
            var bigBangService = _serviceProvider.GetRequiredService<BigBangService>();
            return bigBangService?.Id != null;
        }
        [HttpGet]
        public async Task<int> MultipleRebuildAsync([FromQuery] int max, [FromQuery] bool withParallel = false)
        {
            var services = new List<Type>();
            for (var i = 0; i < max; i++)
            {
                services.Add(typeof(AddedService).Mock(configuration =>
                {
                    configuration.IsSealed = false;
                    configuration.CreateNewOneIfExists = true;
                })!);
            }
            if (!withParallel)
            {
                List<Task> tasks = new();
                foreach (var service in services)
                {
                    tasks.Add(RunAsync());

                    async Task RunAsync()
                    {
                        try
                        {
                            await RuntimeServiceProvider.GetServiceCollection()
                                .AddSingleton(service)
                                .RebuildAsync();
                        }
                        catch (Exception ex)
                        {
                            var olaf = ex.Message;
                        }
                    }
                }
                await Task.WhenAll(tasks);
            }
            else
            {
                var result = Parallel.ForEach(services, async service =>
                {
                    try
                    {
                        await RuntimeServiceProvider
                            .AddServicesToServiceCollectionWithLock(configureFurtherServices =>
                            {
                                configureFurtherServices.AddSingleton(service);
                            })
                        .RebuildAsync();
                    }
                    catch (Exception ex)
                    {
                        var olaf = ex.Message;
                    }
                });
                while (!result.IsCompleted)
                {
                    await Task.Delay(100);
                }
                await Task.Delay(2000);
            }
            var counter = 0;
            foreach (var service in services)
            {
                if (RuntimeServiceProvider.GetServiceProvider().GetService(service) != null)
                    counter++;
                else
                {
                    var olaf = string.Empty;
                }
            }
            return counter;
        }
        [HttpGet]
        public async Task<int> MultipleFactoryAsync([FromQuery] int max, [FromQuery] bool withParallel = false)
        {
            var factoryNames = new List<string>();
            for (var i = 0; i < max; i++)
            {
                factoryNames.Add(Guid.NewGuid().ToString());
            }
            await RuntimeServiceProvider.RebuildAsync();
            if (!withParallel)
            {
                List<Task> tasks = new();
                foreach (var factoryName in factoryNames)
                {
                    tasks.Add(RunAsync());

                    async Task RunAsync()
                    {
                        await Task.Delay(0);
                        var factory = RuntimeServiceProvider.GetServiceProvider().GetRequiredService<IFactory<Factorized>>();
                        factory.Create(factoryName);
                    }
                }
                await Task.WhenAll(tasks);
            }
            else
            {
                var result = Parallel.ForEach(factoryNames, async factoryName =>
                {
                    try
                    {
                        await Task.Delay(0);
                        var factory = RuntimeServiceProvider.GetServiceProvider().GetRequiredService<IFactory<Factorized>>();
                        factory.Create(factoryName);
                    }
                    catch (Exception ex)
                    {
                        var olaf = ex.Message;
                    }
                });
                while (!result.IsCompleted)
                {
                    await Task.Delay(100);
                }
                await Task.Delay(2000);
            }
            var counter = 0;
            foreach (var factoryName in factoryNames)
            {
                var factory = RuntimeServiceProvider.GetServiceProvider().GetRequiredService<IFactory<Factorized>>();
                var serviceFromFactory = factory.Create(factoryName);
                if (serviceFromFactory != null)
                {
                    counter++;
                }
                else
                {
                    var olaf = string.Empty;
                }

            }
            return counter;
        }
        [HttpGet]
        public async Task<int> MultipleFactoryWithMultipleServicesAsync([FromQuery] int max, [FromQuery] bool withParallel = false)
        {
            var factoryName = Guid.NewGuid().ToString();
            var services = new List<Type>();
            for (var i = 0; i < max; i++)
            {
                var serviceType = typeof(Factorized).Mock(configuration =>
                {
                    configuration.IsSealed = false;
                    configuration.CreateNewOneIfExists = true;
                })!;
                services.Add(serviceType);
                RuntimeServiceProvider.AddServicesToServiceCollectionWithLock(t =>
                {
                    t.AddActionAsFallbackWithServiceCollectionRebuilding(services.Last(), async x =>
                    {
                        await Task.Delay(1);
                        var singletonService = x.ServiceProvider.GetService<SingletonService>();
                        if (singletonService != null)
                            x.ServiceColletionBuilder = (serviceCollection => serviceCollection.AddFactory(serviceType, x.Name));
                    });
                });
            }
            await RuntimeServiceProvider.RebuildAsync();
            if (!withParallel)
            {
                List<Task> tasks = new();
                foreach (var service in services)
                {
                    tasks.Add(RunAsync());

                    async Task RunAsync()
                    {
                        await Task.Delay(1);
                        var factoryType = typeof(IFactory<>).MakeGenericType(service);
                        var factory = RuntimeServiceProvider.GetServiceProvider().GetRequiredService(factoryType);
                        var methodInfoForCreation = factory.GetType().GetMethod("Create");
                        _ = methodInfoForCreation!.Invoke(factory, [factoryName]);
                    }
                }
                await Task.WhenAll(tasks);
            }
            else
            {
                var result = Parallel.ForEach(services, async service =>
                {
                    try
                    {
                        var factoryType = typeof(IFactory<>).MakeGenericType(service);
                        var factory = RuntimeServiceProvider.GetServiceProvider().GetRequiredService(factoryType);
                        var methodInfoForCreation = factory.GetType().GetMethod("Create");
                        _ = methodInfoForCreation!.Invoke(factory, [factoryName]);
                    }
                    catch (Exception ex)
                    {
                        var olaf = ex.Message;
                    }
                });
                while (!result.IsCompleted)
                {
                    await Task.Delay(100);
                }
                await Task.Delay(2000);
            }
            var counter = 0;
            foreach (var service in services)
            {
                var factoryType = typeof(IFactory<>).MakeGenericType(service);
                var factory = RuntimeServiceProvider.GetServiceProvider().GetRequiredService(factoryType);
                if (factory != null)
                {
                    var methodInfoForCreation = factory.GetType().GetMethod("Create");
                    var serviceFromFactory = methodInfoForCreation!.Invoke(factory, [factoryName]);
                    if (serviceFromFactory != null)
                    {
                        counter++;
                    }
                    else
                    {
                        var olaf = string.Empty;
                    }
                }
                else
                {
                    var olaf = string.Empty;
                }
            }
            return counter;
        }
    }
}
