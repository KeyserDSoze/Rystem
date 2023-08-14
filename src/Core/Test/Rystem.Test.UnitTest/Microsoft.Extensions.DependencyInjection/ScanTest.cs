using System.Linq;
using System.Reflection;
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
            service.Scan(ServiceLifetime.Scoped, Assembly.GetExecutingAssembly());
            var actualService = service.Last();
            Assert.Equal(ServiceLifetime.Singleton, actualService.Lifetime);
        }
    }
}
