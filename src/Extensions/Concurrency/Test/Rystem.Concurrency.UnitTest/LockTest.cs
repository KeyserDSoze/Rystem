using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Concurrency.Test.UnitTest
{
    public class LockTest
    {
        private int _counter;
        private static readonly IServiceProvider _serviceProvider;
        static LockTest()
        {
            IServiceCollection services = new ServiceCollection()
                .AddLock();
            _serviceProvider = services.BuildServiceProvider();
        }
        [Fact]
        public async Task SingleRun()
        {
            var locking = _serviceProvider.CreateScope().ServiceProvider.GetService<ILock>();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
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