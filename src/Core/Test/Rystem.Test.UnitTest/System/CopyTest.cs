using System;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Test.UnitTest
{
    public class CopyTest
    {
        private class A
        {
            public int Id { get; set; }
        }
        private sealed class B : A { }
        [Fact]
        public async Task Test1()
        {
            B b = new B()
            {
                Id = 3
            };
            B a = b.ToDeepCopy();
            Assert.False(a == b);
            Assert.True(a.Id == b.Id);
        }
        [Fact]
        public async Task Test2()
        {
            B b = new B()
            {
                Id = 3
            };
            B a = b.ToDeepCopy();
            Assert.False(a == b);
            Assert.True(a.Id == b.Id);
            B c = new B();
            object e = new B();
            B d = c;
            c.CopyPropertiesFrom(b);
            Assert.True(c == d);
            Assert.Equal(b.Id, c.Id);
            Assert.Equal(b.Id, d.Id);
            e.CopyPropertiesFrom(d);
            Assert.Equal(b.Id, ((dynamic)e).Id);
            Assert.True(c == d);
        }
    }
}