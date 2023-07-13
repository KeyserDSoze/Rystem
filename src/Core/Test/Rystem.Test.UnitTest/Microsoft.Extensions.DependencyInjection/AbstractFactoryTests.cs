using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Rystem.Test.UnitTest.Microsoft.Extensions.DependencyInjection
{
    public class SingletonOption
    {
        public string ServiceName { get; set; }
    }
    public class TransientOption
    {
        public string ServiceName { get; set; }
    }
    public class ScopedOption
    {
        public string ServiceName { get; set; }
    }
    public class BuiltScopedOptions : IServiceOptions<ScopedOption>
    {
        public string ServiceName { get; set; }

        public Task<Func<ScopedOption>> BuildAsync()
        {
            return Task.FromResult(() => new ScopedOption
            {
                ServiceName = ServiceName
            });
        }
    }
    public class SingletonService : IMyService, IServiceWithOptions<SingletonOption>
    {
        public SingletonOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();
        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public class TransientService : IMyService, IServiceWithOptions<TransientOption>
    {
        public TransientOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();
        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public class ScopedService : IMyService, IServiceWithOptions<ScopedOption>
    {
        public ScopedOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();
        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public class ScopedService2 : IMyService, IServiceWithOptions<ScopedOption>
    {
        public ScopedOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();

        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public class ScopedService3 : IMyService, IServiceWithOptions<ScopedOption>
    {
        public ScopedOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();

        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public class ScopedService4 : IMyService, IServiceWithOptions<ScopedOption>
    {
        public ScopedOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();

        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public interface IMyService
    {
        string GetName();
        string Id { get; }
    }
    public class AbstractFactoryTests
    {
        [Fact]
        public async Task RunAsync()
        {
            var services = new ServiceCollection();
            services.AddFactory<IMyService, SingletonService, SingletonOption>(x =>
            {
                x.ServiceName = "singleton";
            },
            "singleton",
            ServiceLifetime.Singleton);

            services.AddFactory<IMyService, TransientService, TransientOption>(x =>
            {
                x.ServiceName = "transient";
            },
            "transient",
            ServiceLifetime.Transient);

            services.AddFactory<IMyService, ScopedService, ScopedOption>(x =>
            {
                x.ServiceName = "scoped";
            },
            "scoped",
            ServiceLifetime.Scoped);

            services.AddFactory<IMyService, ScopedService2, ScopedOption>(x =>
            {
                x.ServiceName = "scoped2";
            },
            "scoped2",
            ServiceLifetime.Scoped);

            await services.AddFactoryAsync<IMyService, ScopedService3, BuiltScopedOptions, ScopedOption>(
                x =>
                {
                    x.ServiceName = "scoped3";
                },
                "scoped3"
            );

            await services.AddFactoryAsync<IMyService, ScopedService3, BuiltScopedOptions, ScopedOption>(
               x =>
               {
                   x.ServiceName = "scoped3_2";
               },
               "scoped3_2"
           );

            await services.AddFactoryAsync<IMyService, ScopedService4, BuiltScopedOptions, ScopedOption>(
               x =>
               {
                   x.ServiceName = "scoped4";
               },
               "scoped4"
           );

            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            var factory = serviceProvider.GetService<IFactory<IMyService>>()!;
            var factory2 = serviceProvider.GetService<IFactory<IMyService>>()!;

            var singletonFromFactory = factory.Create("singleton").Id;
            var singletonFromFactory2 = factory2.Create("singleton").Id;
            var transientFromFactory = factory.Create("transient").Id;
            var transientFromFactory2 = factory2.Create("transient").Id;
            var scopedFromFactory = factory.Create("scoped").Id;
            var scopedFromFactory2 = factory2.Create("scoped").Id;
            var scoped2FromFactory = factory.Create("scoped2").Id;
            var scoped2FromFactory2 = factory2.Create("scoped2").Id;
            var scoped3FromFactory = factory.Create("scoped3").Id;
            var scoped3FromFactory2 = factory2.Create("scoped3").Id;
            var scoped3_2FromFactory = factory.Create("scoped3_2").Id;
            var scoped3_2FromFactory2 = factory2.Create("scoped3_2").Id;
            var scoped4FromFactory = factory.Create("scoped4").Id;
            var scoped4FromFactory2 = factory2.Create("scoped4").Id;

            Assert.Equal(singletonFromFactory, singletonFromFactory2);
            Assert.NotEqual(transientFromFactory, transientFromFactory2);
            Assert.Equal(scopedFromFactory, scopedFromFactory2);
            Assert.Equal(scoped2FromFactory, scoped2FromFactory2);
            Assert.NotEqual(scoped3FromFactory, scoped3FromFactory2);
            Assert.NotEqual(scoped3_2FromFactory, scoped3_2FromFactory2);
            Assert.NotEqual(scoped4FromFactory, scoped4FromFactory2);
        }
    }
}
