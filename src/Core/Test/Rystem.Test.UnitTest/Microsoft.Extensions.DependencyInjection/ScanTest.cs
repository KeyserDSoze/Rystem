using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Rystem.Test.UnitTest.DependencyInjection
{
    public class ScanTest
    {
        [Fact]
        public void Run()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ScanDependencyContext(ServiceLifetime.Scoped);
            serviceCollection.ScanDependencyContext(ServiceLifetime.Scoped);
            var actualService = serviceCollection.Last();
            Assert.Equal(ServiceLifetime.Singleton, actualService.Lifetime);
            Assert.Single(serviceCollection);
        }
    }
}
