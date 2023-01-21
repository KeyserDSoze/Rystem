using System.Reflection;
using Xunit;

namespace Rystem.Test.UnitTest.Reflection
{
    public class MockTest
    {
        public abstract class Alzio
        {
            private protected string X { get; }
            public string O => X;
            public string A { get; set; }
            public Alzio(string x) => X = x;
        }
        [Fact]
        public void Test1()
        {
            var mocked = typeof(Alzio).CreateInstance("AAA") as Alzio;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            mocked.A = "rrrr";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.Equal("AAA", mocked.O);
            Assert.Equal("rrrr", mocked.A);
        }
        [Fact]
        public void Test2()
        {
            Alzio alzio = null!;
            var mocked = alzio.CreateInstance("AAA");
            mocked.A = "rrrr";
            Assert.Equal("AAA", mocked.O);
            Assert.Equal("rrrr", mocked.A);
        }
    }
}