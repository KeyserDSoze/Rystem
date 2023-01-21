using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Test.UnitTest.DependencyInjection
{
    public class DependencyInjectionTest
    {
        public class Foo
        {
            public string Hello() => "Hello world";
        }
        public class Foo2
        {
            public string Hello() => "Hello world";
        }

        [Fact]
        public async Task RunAsync()
        {
            var service = new ServiceCollection();
            service.AddSingleton<Foo>();
            service.AddSingleton<Foo2>();
            service.AddWarmUp(x =>
            {
                var q = x.GetService<Foo>()!.Hello() + x.GetService<Foo2>()!.Hello();
                Assert.Equal("Hello worldHello world", q);
                return Task.CompletedTask;
            });
            var response = await service.ExecuteUntilNowAsync((Foo x) =>
            {
                return Task.FromResult(x.Hello());
            }).NoContext();
            Assert.Equal("Hello world", response);
            response = await service.ExecuteUntilNowAsync((IServiceProvider x) =>
            {
                return Task.FromResult(x.GetService<Foo>()!.Hello() + x.GetService<Foo2>()!.Hello());
            }).NoContext();
            Assert.Equal("Hello worldHello world", response);
            await service.BuildServiceProvider().CreateScope().ServiceProvider
                .WarmUpAsync();
        }
    }
}
