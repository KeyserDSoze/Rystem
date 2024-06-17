using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Rystem.Test.TestApi.Models;

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
                     .ReBuildAsync();
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
        public async Task<int> MultipleRebuildAsync([FromQuery] int max)
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
            var result = Parallel.ForEach(services, async service =>
            {
                await RuntimeServiceProvider.GetServiceCollection()
                    .AddSingleton(service)
                    .ReBuildAsync();
            });
            while (!result.IsCompleted)
            {
                await Task.Delay(100);
            }
            var counter = 0;
            foreach (var service in services)
            {
                if (RuntimeServiceProvider.GetServiceProvider().GetService(service) != null)
                    counter++;
            }
            return counter;
        }
    }
}
