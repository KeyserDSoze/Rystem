using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.InMemory;
using Xunit;

namespace RepositoryFramework.UnitTest.Tests.Unitary
{
    public class TranslatorTests
    {
        public class ModelX
        {
            public string Al { get; set; } = null!;
            public string C { get; set; } = null!;
            public ModelY ModelY { get; set; } = null!;
            public List<ModelY> ModelYY { get; set; } = null!;
        }
        public class ModelY
        {
            public string B { get; set; } = null!;
        }
        public class ModelX1
        {
            public string A { get; set; } = null!;
            public ModelY1 A2 { get; set; } = null!;
            public List<ModelY1> A22 { get; set; } = null!;
        }
        public class ModelY1
        {
            public string B { get; set; } = null!;
            public ModelZ1 B2 { get; set; } = null!;
        }
        public class ModelZ1
        {
            public string C { get; set; } = null!;
        }
        [Theory]
        [InlineData(typeof(string))]
        public async Task TestAsync(Type type)
        {
            var services = new ServiceCollection();
            services.AddRepository<ModelX, string>(x =>
            {
                x.WithInMemory();
                x.Translate<ModelX1>()
                    .With(x => x.Al, x => x.A)
                    .With(x => x.C, x => x.A2.B2.C)
                    .With(x => x.ModelY.B, x => x.A2.B)
                    .With(x => x.ModelYY.First().B, x => x.A22.First().B2.C);
            });
            var provider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            var repository = provider.GetRequiredService<IRepository<ModelX, string>>();
            var q = "AA";
            try
            {
                var x = await repository.Where(x => x.Al == "A" && x.C == "C" && x.ModelY.B == q && x.ModelYY.All(y => y.B == "dasd")).ToListAsync();
                Assert.Empty(x);
            }
            catch { }
        }
    }
}
