using System;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Test.UnitTest
{
    public class CastTest
    {
        private class A
        {
            public int Id { get; set; }
        }
        private sealed class B : A { }
        [Fact]
        public async Task Test1()
        {
            int x = 2;
            var result = x.Cast<decimal>();
            Assert.Equal(2M, result);
            int? x2 = null;
            var result2 = x2.Cast<decimal>();
            Assert.Equal(0, result2);
            var result3 = x2.Cast<decimal?>();
            Assert.Null(result3);
            B b = new B() { Id = 4 };
            var result4 = b.Cast<A>();
            Assert.NotNull(result4);
            Assert.Equal(typeof(B), result4!.GetType());
            Assert.Equal(4, result4!.Id);
            B? b2 = null;
            var result5 = b2.Cast<A>();
            Assert.Null(result5);
            var guid = Guid.NewGuid().ToString();
            var result6 = guid.Cast<Guid>();
            Assert.Equal(guid, result6.ToString());
        }
    }
}