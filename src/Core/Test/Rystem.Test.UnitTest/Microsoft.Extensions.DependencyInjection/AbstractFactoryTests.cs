using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Rystem.Test.UnitTest.DependencyInjection
{
    public interface ITestService
    {
        string Id { get; }
        string FactoryName { get; }
    }
    public class TestService : ITestService, IDecoratorService<ITestService>, IServiceWithFactoryWithOptions<TestOptions>
    {
        public string Id => Options.ClassicName;
        public string FactoryName { get; private set; }
        public ITestService Test { get; private set; }
        public TestOptions Options { get; private set; }
        public void SetDecoratedService(ITestService service)
        {
            Test = service;
        }

        public void SetFactoryName(string name)
        {
            FactoryName = name;
        }
        public void SetOptions(TestOptions options)
        {
            Options = options;
        }
    }
    public class TestOptions
    {
        public string ClassicName { get; set; }
    }
    public class DecoratorTestService : ITestService, IDecoratorService<ITestService>, IServiceWithFactoryWithOptions<TestOptions>, IServiceForFactory
    {
        public string Id => $"Decoration {Test.Id} with same Options {Options.ClassicName}";
        public ITestService Test { get; private set; }
        public string FactoryName => $"Decoration {DecoratedFactoryName} with {Test.FactoryName}";
        public string DecoratedFactoryName { get; private set; }
        public void SetFactoryName(string name)
        {
            DecoratedFactoryName = name;
        }
        public void SetDecoratedService(ITestService service)
        {
            Test = service;
        }
        public TestOptions Options { get; private set; }
        public void SetOptions(TestOptions options)
        {
            Options = options;
        }
    }
    public interface ITestWithoutFactoryService
    {
        string Id { get; }
    }
    public class TestWithoutFactoryService : ITestWithoutFactoryService
    {
        public string Id { get; } = Guid.NewGuid().ToString();
    }
    public class TestWithoutFactoryServiceDecorator : ITestWithoutFactoryService, IDecoratorService<ITestWithoutFactoryService>
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public ITestWithoutFactoryService Test { get; private set; }
        public void SetDecoratedService(ITestWithoutFactoryService service)
        {
            Test = service;
        }

        public void SetFactoryName(string name)
        {
            return;
        }
    }
    public class AbstractFactoryTests
    {
        [Theory]
        [InlineData("singleton", ServiceLifetime.Singleton, "classicName")]
        [InlineData("scoped", ServiceLifetime.Scoped, "classicName")]
        [InlineData("transient", ServiceLifetime.Transient, "classicName")]
        public async Task RunAsync(string name, ServiceLifetime lifetime, string classicName)
        {
            var services = new ServiceCollection();
            try
            {
                services
                    .AddDecoration<ITestWithoutFactoryService, TestWithoutFactoryServiceDecorator>(null, lifetime);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.StartsWith("It's not possible to override a service not installed", ex.Message);
            }
            try
            {
                services
                    .AddDecoration<ITestService, DecoratorTestService>(name, lifetime);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.StartsWith("It's not possible to override a service not installed", ex.Message);
            }

            services.AddFactory<ITestService, TestService, TestOptions>(x =>
            {
                x.ClassicName = classicName;
            },
            name,
            lifetime);
            services
                .AddDecoration<ITestService, DecoratorTestService>(name, lifetime);
            services
                .AddService<ITestWithoutFactoryService, TestWithoutFactoryService>(lifetime);
            services
                   .AddDecoration<ITestWithoutFactoryService, TestWithoutFactoryServiceDecorator>(null, lifetime);

            _ = string.Join('\n', services.Select(x => x.ServiceType.FullName));
            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;

            var factory = serviceProvider.GetRequiredService<IFactory<ITestService>>();
            var serviceFromFactoryFromDi = serviceProvider.GetRequiredService<ITestService>();

            Assert.NotNull(serviceFromFactoryFromDi.FactoryName);
            Assert.NotNull(((dynamic)serviceFromFactoryFromDi).Test);
            var serviceFromFactory = factory.Create(name);

            var decorator = serviceProvider.GetRequiredService<ITestWithoutFactoryService>();
            var decorated = serviceProvider.GetRequiredService<IDecoratedService<ITestWithoutFactoryService>>();

            var id = $"Decoration {classicName} with same Options {classicName}";
            var decoratedName = $"Decoration {name} with {name}";
            Assert.Equal(decoratedName, serviceFromFactoryFromDi.FactoryName);
            Assert.Equal(decoratedName, serviceFromFactory.FactoryName);
            Assert.Equal(id, serviceFromFactoryFromDi.Id);
            Assert.Equal(id, serviceFromFactory.Id);
            Assert.NotNull(decorated.Service.Id);
            Assert.NotNull(decorator.Id);

            if (lifetime != ServiceLifetime.Transient)
                Assert.Equal(((dynamic)decorator).Test.Id, decorated.Service.Id);
            else
                Assert.NotEqual(((dynamic)decorator).Test.Id, decorated.Service.Id);
        }
    }
}
