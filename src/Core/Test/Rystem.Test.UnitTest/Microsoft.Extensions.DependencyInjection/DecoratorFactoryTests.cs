using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.UnitTest.Microsoft.Extensions.DependencyInjection.Decoration;
using Xunit;

namespace Rystem.Test.UnitTest.DependencyInjection
{
    public class DecoratorFactoryTests
    {
        [Theory]
        [InlineData("singleton", ServiceLifetime.Singleton)]
        [InlineData("scoped", ServiceLifetime.Scoped)]
        [InlineData("transient", ServiceLifetime.Transient)]
        public async Task RunWithStringKeyAsync(string name, ServiceLifetime lifetime)
        {
            await RunAsync(name, lifetime);
        }
        [Theory]
        [InlineData(TestEnum.Singleton, ServiceLifetime.Transient)]
        [InlineData(TestEnum.Scoped, ServiceLifetime.Transient)]
        [InlineData(TestEnum.Transient, ServiceLifetime.Transient)]
        public async Task RunWithEnumKeyAsync(TestEnum name, ServiceLifetime lifetime)
        {
            await RunAsync(name, lifetime);
        }
        private async Task RunAsync(AnyOf<string, Enum> name, ServiceLifetime lifetime)
        {
            await Task.Delay(0);
            var services = new ServiceCollection();
            services.AddFactory<IRepositoryPattern<ToSave>, RepositoryPattern<ToSave>>(
                name,
                lifetime);
            services.AddFactory<IRepository<ToSave>, Repository<ToSave>, RepositoryOptions>(x =>
            {
                x.Name = "Hello";
            },
            name,
            lifetime);
            services
                .AddDecoration<IRepository<ToSave>, Cache<ToSave>>(name, lifetime);
            services
                .AddService<ITestWithoutFactoryService, TestWithoutFactoryService>(lifetime);
            services
                   .AddDecoration<ITestWithoutFactoryService, TestWithoutFactoryServiceDecorator>(null, lifetime);

            var providerWithoutScope = services.BuildServiceProvider();
            var provider = providerWithoutScope.CreateScope().ServiceProvider;

            var decorator = provider.GetService<IRepository<ToSave>>();
            Assert.Null(decorator.Get());
            decorator.Format = "Alzo";

            var provider2 = providerWithoutScope.CreateScope().ServiceProvider;
            var decorator2 = provider2.GetService<IRepository<ToSave>>();
            if (lifetime == ServiceLifetime.Singleton)
            {
                Assert.Equal("Alzo", decorator2.Format);
            }
            else
            {
                Assert.NotEqual("Alzo", decorator2.Format);
            }

        }
    }
}
