using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Rystem.Test.UnitTest.Reflection
{
    public class ReflectionTest
    {
        public class Zalo : Sulo
        {

        }
        public class Sulo
        {

        }
        public class Folli : Zalo
        {

        }
        public class Foo
        {
            public IEnumerable<string> Values { get; }
            public bool X { get; }
            public void FooToon()
            {

            }
            public Foo(IEnumerable<string> values)
            {
                Values = values;
            }
            public Foo(IEnumerable<string> values, bool x)
            {
                Values = values;
                X = x;
            }
        }
        public class Foo2
        {
            public IEnumerable<string> Values { get; }
            public bool X { get; }
            public Dictionary<string, string> Complex { get; init; }
            public HashSet<int> Integers { get; init; }
            private readonly Foo _tiny;
            public Foo Tiny => _tiny;
            public void FooToon()
            {

            }
            public Foo2(IEnumerable<string> values)
            {
                Values = values;
            }
            public Foo2(IEnumerable<string> values, bool x)
            {
                Values = values;
                X = x;
            }
        }
        [Fact]
        public void IsTheSameTypeOrASonTest()
        {
            Zalo zalo = new();
            Zalo zalo2 = new();
            Folli folli = new();
            Sulo sulo = new();
            object quo = new();
            int x = 2;
            decimal y = 3;
            Assert.True(zalo.IsTheSameTypeOrASon(sulo));
            Assert.True(folli.IsTheSameTypeOrASon(sulo));
            Assert.True(zalo.IsTheSameTypeOrASon(zalo2));
            Assert.True(zalo.IsTheSameTypeOrASon(quo));
            Assert.False(sulo.IsTheSameTypeOrASon(zalo));
            Assert.True(sulo.IsTheSameTypeOrAParent(zalo));
            Assert.False(y.IsTheSameTypeOrAParent(x));
        }
        [Fact]
        public void IsTheSameTypeOrAParentTest()
        {
            Zalo zalo = new();
            Sulo sulo = new();
            int x = 2;
            decimal y = 3;
            Assert.True(sulo.IsTheSameTypeOrAParent(zalo));
            Assert.False(y.IsTheSameTypeOrAParent(x));
        }
        [Fact]
        public void CreateInstance()
        {
            var instance = typeof(Foo).CreateWithDefault<Foo>()!;
            Assert.NotNull(instance);
            //instance.Values = new List<string>();
            (instance.Values as List<string>)!.Add("aaa");
            Assert.Equal("aaa", instance.Values.First());
        }
        [Fact]
        public void CreateInstanceWithDefaultConstructorPropertyAndField()
        {
            var instance = typeof(Foo2).CreateWithDefaultConstructorPropertiesAndField<Foo2>()!;
            Assert.NotNull(instance);
            //instance.Values = new List<string>();
            (instance.Values as List<string>)!.Add("aaa");
            Assert.Equal("aaa", instance.Values.First());
            instance.Complex.Add("x", "y");
            Assert.Equal(1, instance.Complex.Count!);
            Assert.NotNull(instance.Tiny);
            (instance.Tiny.Values as List<string>)!.Add("aaa");
            Assert.Equal("aaa", instance.Tiny.Values.First());
            Assert.Equal(0, instance.Integers.Count!);
        }
    }
}
