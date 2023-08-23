using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Rystem.Test.UnitTest.Reflection
{
    public class NullableTest
    {
        private sealed class InModel
        {
            public string? A { get; set; }
            public string B { get; set; }
            public string? C;
            public string D;
            public InModel(string? b, string c)
            {
                A = b;
                B = c;
            }
            public void SetSomething(string? b, string c)
            {
                A = b;
                B = c;
            }
        }
        [Fact]
        public void Test1()
        {
            var type = typeof(InModel);
            var constructorParameters = type.GetConstructors().First().GetParameters().ToList();
            Assert.True(constructorParameters[0].IsNullable());
            Assert.False(constructorParameters[1].IsNullable());
            var methodParameters = type.GetMethod(nameof(InModel.SetSomething)).GetParameters().ToList();
            Assert.True(methodParameters[0].IsNullable());
            Assert.False(methodParameters[1].IsNullable());
            var properties = type.GetProperties().ToList();
            Assert.True(properties[0].IsNullable());
            Assert.False(properties[1].IsNullable());
            var fields = type.GetFields().ToList();
            Assert.True(fields[0].IsNullable());
            Assert.False(fields[1].IsNullable());
        }
    }
}
