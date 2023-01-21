using System;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Test.UnitTest
{
    public class TryCatchTest
    {
        [Fact]
        public void Test1()
        {
            var t = Try.WithDefaultOnCatch(() =>
             {
                 throw new Exception();
#pragma warning disable CS0162 // Unreachable code detected
                 return 1;
#pragma warning restore CS0162 // Unreachable code detected
             });
            Assert.Equal(0, t);
        }
        [Fact]
        public void Test2()
        {
            var t = Try.WithDefaultOnCatch(() =>
            {
                return 1;
            });
            Assert.Equal(1, t);
        }
        [Fact]
        public async Task Test3()
        {
            var t = await Try.WithDefaultOnCatchAsync(() =>
            {
                throw new Exception();
#pragma warning disable CS0162 // Unreachable code detected
                return Task.FromResult(1);
#pragma warning restore CS0162 // Unreachable code detected
            });
            Assert.Equal(0, t);
        }
        [Fact]
        public async Task Test4()
        {
            var t = await Try.WithDefaultOnCatchAsync(() =>
             {
                 return Task.FromResult(1);
             });
            Assert.Equal(1, t);
        }
    }
}