using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Rystem.Test.UnitTest.Enumerable
{
    public class SystemEnumerable
    {
        internal sealed class Something
        {
            public string A { get; set; }
        }
        [Fact]
        public void TestWithList()
        {
            List<Something> makes = new();
            for (int i = 0; i < 100; i++)
                makes.Add(new Something { A = i.ToString() });
            int[] arrays = (int[])Array.CreateInstance(typeof(int), 100);
            for (int i = 0; i < arrays.Length; i++)
                arrays[i] = i;

            IEnumerable enumerable = makes;
            dynamic value = enumerable.ElementAt(10)!;
            Assert.Equal("10", value.A);
            enumerable.SetElementAt(10, new Something { A = "set" });
            value = enumerable.ElementAt(10)!;
            Assert.Equal("set", value.A);
            Assert.True(enumerable.RemoveElementAt(10, out enumerable, out value));
            Assert.Equal("set", value.A);
            value = enumerable.ElementAt(10)!;
            Assert.Equal("11", value.A);

            enumerable = arrays;
            value = enumerable.ElementAt(10);
            Assert.Equal(10, value);
            enumerable.SetElementAt(10, 12_000);
            value = enumerable.ElementAt(10);
            Assert.Equal(12_000, value);
            Assert.True(enumerable.RemoveElementAt(10, out enumerable, out value));
            Assert.Equal(12_000, value);
            value = enumerable.ElementAt(10)!;
            Assert.Equal(11, value);
        }
    }
}