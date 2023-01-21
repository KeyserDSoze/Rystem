using System;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Test.UnitTest
{
    public class StopwatchTest
    {
        [Fact]
        public async Task Test1()
        {
            var started = Stopwatch.Start();
            await Task.Delay(2000);
            var result = started.Stop();
            Assert.True(result.Span > TimeSpan.FromSeconds(2));
        }
        [Fact]
        public async Task Test2()
        {
            var result = await Stopwatch.MonitorAsync(async () =>
            {
                await Task.Delay(2000);
            });
            Assert.True(result.Span > TimeSpan.FromSeconds(2));
        }
        [Fact]
        public async Task Test3()
        {
            var result = await Stopwatch.MonitorAsync(async () =>
            {
                await Task.Delay(2000);
                return 3;
            });
            Assert.True(result.Stopwatch.Span > TimeSpan.FromSeconds(2));
            Assert.Equal(3, result.Result);
        }
    }
}