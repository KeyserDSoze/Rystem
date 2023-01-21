using System;
using System.Reflection;
using Xunit;

namespace Rystem.Test.UnitTest.Reflection
{
    public class PrimitiveTest
    {
        public class Zalo
        {

        }
        public record Sulo
        {

        }
        [Fact]
        public void Test()
        {
            bool a = true;
            Assert.True(a.IsPrimitive());
            bool? b = null;
            Assert.True(b.IsPrimitive());
            string c = null!;
            Assert.True(c.IsPrimitive());
            string d = "dasdsad";
            Assert.True(d.IsPrimitive());
            int? e = 32;
            Assert.True(e.IsPrimitive());
            Range range = new(2, 3);
            Assert.False(range.IsPrimitive());
            Zalo zalo = new();
            Assert.False(zalo.IsPrimitive());
            Sulo sulo = new();
            Assert.False(sulo.IsPrimitive());
            object k = new();
            Assert.False(k.IsPrimitive());

        }
    }
}
