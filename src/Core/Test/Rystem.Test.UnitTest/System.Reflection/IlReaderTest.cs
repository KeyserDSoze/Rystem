using System;
using System.Reflection;
using Xunit;

namespace Rystem.Test.UnitTest.Reflection
{
    public class IlReaderTest
    {

        public class Sulo
        {
            public string Something()
            {
                return "dddd";
            }
            public string Something2()
            {
                throw new NotImplementedException();
            }
            private string Soly(int x, Func<int, bool> y)
            {
                return y.Invoke(x).ToString();
            }
            public string Something3()
            {
                return Soly(1, x =>
                {
                    return x == 1;
                });
            }
        }

        [Fact]
        public void Test()
        {
            var method = typeof(Sulo).GetMethod(nameof(Sulo.Something), BindingFlags.Public | BindingFlags.Instance);
            var value = method.GetBodyAsString();
            Assert.Contains("0001 : ldstr \"dddd\"", value);
            method = typeof(Sulo).GetMethod(nameof(Sulo.Something2), BindingFlags.Public | BindingFlags.Instance);
            value = method.GetBodyAsString();
            Assert.Contains("newobj instance void System.NotImplementedException", value);
        }
        [Fact]
        public void Test2()
        {
            var method = typeof(Sulo).GetMethod(nameof(Sulo.Something), BindingFlags.Public | BindingFlags.Instance);
            var value = method.GetInstructions();
            Assert.Equal(value[1].Operand, "dddd");
            method = typeof(Sulo).GetMethod(nameof(Sulo.Something2), BindingFlags.Public | BindingFlags.Instance);
            value = method.GetInstructions();
            Assert.Equal((value[1].Operand as dynamic).DeclaringType.Name, typeof(NotImplementedException).Name);
        }
        [Fact]
        public void Test3()
        {
            var method = typeof(Sulo).GetMethod(nameof(Sulo.Something3), BindingFlags.Public | BindingFlags.Instance);
            var value = method.GetInstructions();
            Assert.Equal(value[1].Operand, "dddd");
        }
    }
}
