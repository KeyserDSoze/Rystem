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
        public class GenericSulo<T>
        {
            public T Something()
            {
                return default;
            }
            public T Something2()
            {
                throw new NotImplementedException();
            }
            public T Soly<F>(int x, Func<int, F> y)
            {
                var f = y.Invoke(x);
                return default;
            }
            public T Soly2<F>(int x, Func<int, F> y)
            {
                throw new NotImplementedException();
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
            var method = typeof(Sulo).GetMethod(nameof(Sulo.Something), BindingFlags.Public | BindingFlags.Instance);
            var value = method.GetInstructions();
            Assert.Equal(value[1].Operand, "dddd");
        }
        [Fact]
        public void GenericTest()
        {
            var method = typeof(GenericSulo<>).GetMethod(nameof(GenericSulo<int>.Something), BindingFlags.Public | BindingFlags.Instance);
            var value = method.GetInstructions();
            Assert.Equal(value[1].Operand, (byte)0);
            method = typeof(GenericSulo<>).GetMethod(nameof(GenericSulo<int>.Something2), BindingFlags.Public | BindingFlags.Instance);
            value = method.GetInstructions();
            Assert.Equal((value[1].Operand as dynamic).DeclaringType.Name, typeof(NotImplementedException).Name);
            method = typeof(GenericSulo<>).GetMethod(nameof(GenericSulo<int>.Soly), BindingFlags.Public | BindingFlags.Instance);
            //.MakeGenericMethod(typeof(Sulo));
            value = method.GetInstructions();
            Assert.Equal(value[1].Operand, null);
            method = typeof(GenericSulo<>).GetMethod(nameof(GenericSulo<int>.Soly2), BindingFlags.Public | BindingFlags.Instance);
            value = method.GetInstructions();
            Assert.Equal((value[1].Operand as dynamic).DeclaringType.Name, typeof(NotImplementedException).Name);
        }
    }
}
