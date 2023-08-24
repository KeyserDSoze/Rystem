using System;
using System.Linq;
using System.Population.Random;
using System.Reflection;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RepositoryFramework.UnitTest.Tests.Unitary
{
    public class KeyTests
    {
        public class DefaultKey : IDefaultKey
        {
            public string A { get; set; }
            public int B { get; set; }
            public double C { get; set; }
        }
        public class ClassicKey : IKey
        {
            public string A { get; set; }
            public int B { get; set; }
            public double C { get; set; }

            public static IKey Parse(string keyAsString)
            {
                var splitted = keyAsString.Split('$');
                return new ClassicKey { A = splitted[0], B = int.Parse(splitted[1]), C = double.Parse(splitted[2]) };
            }

            public string AsString()
            {
                return $"{A}${B}${C}";
            }
        }
        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(ClassicKey))]
        [InlineData(typeof(DefaultKey))]
        [InlineData(typeof(Guid))]
        public void Test(Type type)
        {
            Generics.WithStatic<KeyTests>(nameof(SingleTest), type).Invoke();
        }
        public static void SingleTest<T>()
            where T : notnull
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddPopulationService();
            var populationService = serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IPopulation<T>>();
            var key = populationService.Populate(1, 1).First();
            var value = KeySettings<T>.Instance.AsString(key);
            var parsedKey = KeySettings<T>.Instance.Parse(value);
            if (typeof(T).IsPrimitive())
            {
                Assert.Equal(key, parsedKey);
            }
            else
            {
                var propertiesFromKey = typeof(T).GetProperties();
                var propertiesFromParsedKey = parsedKey.GetType().GetProperties();
                for (var i = 0; i < propertiesFromKey.Length; i++)
                {
                    var propertyFromKey = propertiesFromKey[i].GetValue(key);
                    var propertyFromParsedKey = propertiesFromParsedKey[i].GetValue(parsedKey);
                    Assert.Equal(propertyFromKey, propertyFromParsedKey);
                }
            }
        }
    }
}
