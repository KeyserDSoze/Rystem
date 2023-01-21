using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Concurrency.Test.UnitTest
{
    public class RaceConditionTest
    {
        private int _counter;
        private static readonly IServiceProvider _serviceProvider;
        static RaceConditionTest()
        {
            IServiceCollection services = new ServiceCollection()
                .AddRaceCondition();
            _serviceProvider = services.BuildServiceProvider();
        }
        [Fact]
        public async Task SingleRun()
        {
            var raceCondition = _serviceProvider.CreateScope().ServiceProvider.GetService<IRaceCodition>();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
                tasks.Add(raceCondition!.ExecuteAsync(() => CountAsync(2), (i % 2).ToString(), TimeSpan.FromSeconds(2)));

            await Task.WhenAll(tasks);
            Assert.Equal(4, _counter);
        }
        private async Task CountAsync(int v)
        {
            await Task.Delay(15);
            _counter += v;
        }
    }
}