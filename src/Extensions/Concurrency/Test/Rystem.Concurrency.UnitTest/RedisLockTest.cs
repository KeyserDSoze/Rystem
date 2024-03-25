using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.UnitTest;
using System;
using System.Collections.Generic;
using System.Threading.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Concurrency.Test.UnitTest
{
    public class RedisLockTest
    {
        private int _counter;
        private static readonly IServiceProvider _serviceProvider;
        static RedisLockTest()
        {
            var services = DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration);
            services.AddRedisLock(x =>
                {
                    x.ConnectionString = configuration["ConnectionString:Redis"]!;
                });
            _serviceProvider = services.BuildServiceProvider();
        }
        [Fact]
        public async Task SingleRun()
        {
            var locking = _serviceProvider.CreateScope().ServiceProvider.GetService<ILock>();
            var tasks = new List<Task>();
            for (var i = 0; i < 100; i++)
                tasks.Add(locking!.ExecuteAsync(() => CountAsync(2)));

            await Task.WhenAll(tasks);
            Assert.Equal(100 * 2, _counter);
        }
        private async Task CountAsync(int v)
        {
            await Task.Delay(15);
            _counter += v;
        }
    }
}
