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
        public async Task RunAsync(string name, ServiceLifetime lifetime)
        {
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
