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
            var service = new ServiceCollection();
            service.ScanDependencyContext(ServiceLifetime.Scoped);
            var actualService = service.Last();
            Assert.Equal(ServiceLifetime.Singleton, actualService.Lifetime);
        }
    }
}
