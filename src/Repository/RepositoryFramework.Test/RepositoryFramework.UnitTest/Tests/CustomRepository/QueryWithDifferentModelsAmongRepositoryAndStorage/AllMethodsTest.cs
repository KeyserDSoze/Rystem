using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.UnitTest.QueryWithDifferentModelsAmongRepositoryAndStorage.Models;
using RepositoryFramework.UnitTest.QueryWithDifferentModelsAmongRepositoryAndStorage.Storage;
using Xunit;

namespace RepositoryFramework.UnitTest.QueryWithDifferentModelsAmongRepositoryAndStorage
{
    public class QueryWithDifferentModelsAmongRepositoryAndStorage
    {
        private static readonly IServiceProvider s_serviceProvider;
        static QueryWithDifferentModelsAmongRepositoryAndStorage()
        {
            DiUtility.CreateDependencyInjectionWithConfiguration(out _)
                .AddRepository<Car, int>(settings =>
                {
                    settings
                        .SetStorage<CarRepository>()
                        .Translate<Auto>()
                            .With(x => x.Id, x => x.Identificativo)
                            .With(x => x.Id2, x => x.Identificativo2)
                            .With(x => x.NumberOfWheels, x => x.NumeroRuote)
                            .With(x => x.Plate, x => x.Targa)
                            .With(x => x.Driver, x => x.Guidatore)
                            .With(x => x.Driver!.Name, x => x.Guidatore!.Nome);
                })
                .AddRepository<Auto, int>(settings =>
                {
                    settings.SetStorage<AutoRepository>();
                })
                .Finalize(out s_serviceProvider);
        }
        private readonly IRepository<Car, int> _repository;
        private readonly IRepository<Auto, int> _repositoryNoTranslation;
        public QueryWithDifferentModelsAmongRepositoryAndStorage()
        {
            _repository = s_serviceProvider.GetService<IRepository<Car, int>>()!;
            _repositoryNoTranslation = s_serviceProvider.GetService<IRepository<Auto, int>>()!;
        }
        private sealed class Referencer
        {
            public int Id { get; set; }
        }
        [Theory]
        [InlineData(0, 5)]
        [InlineData(1, 4)]
        [InlineData(2, 3)]
        [InlineData(3, 2)]
        [InlineData(4, 1)]
        [InlineData(5, 0)]
        public async Task QueryWithDifferentValuesAsync(int minimumId, int numberOfResults)
        {
            var theId = new Referencer
            {
                Id = 4
            };
            var results24 = await _repository.Where(x => x.Id == theId.Id).ToListAsync();
            var results = await _repository
                .Where(x => x.Id > minimumId && x.Id2 > minimumId && !string.IsNullOrWhiteSpace(x.Plate) && x.Plate != null && x.Driver != null && x.Driver.Name == null && string.IsNullOrEmpty(x.O))
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            Assert.Equal(numberOfResults, results.Count);
            if (results.Any())
                Assert.Equal(5, results.First().Value!.Id);
            var results2 = await _repository.ToListAsync();
            Assert.Equal(5, results2.Count);
            var results3 = await _repository
                .OrderBy(x => x.Id)
                .PageAsync(1, 2);
            Assert.Equal(2, results3.Items.Count);
            Assert.Equal(5, results3.TotalCount);
            Assert.Equal(1, results3.Items.First().Value!.Id);
        }
        [Theory]
        [InlineData(0, 5)]
        [InlineData(1, 4)]
        [InlineData(2, 3)]
        [InlineData(3, 2)]
        [InlineData(4, 1)]
        [InlineData(5, 0)]
        public async Task QueryWithDifferentValuesAndNotTranslationAsync(int minimumId, int numberOfResults)
        {
            var theId = new Referencer
            {
                Id = 4
            };
            var results24 = await _repositoryNoTranslation.Where(x => x.Identificativo == theId.Id).ToListAsync();
            var results = await _repositoryNoTranslation
                .Where(x => x.Identificativo > minimumId && x.Identificativo2 > minimumId && !string.IsNullOrWhiteSpace(x.Targa) && x.Targa != null && x.Guidatore != null && x.Guidatore.Nome == null && string.IsNullOrEmpty(x.O))
                .OrderByDescending(x => x.Identificativo)
                .ToListAsync();
            Assert.Equal(numberOfResults, results.Count);
            if (results.Any())
                Assert.Equal(5, results.First().Value!.Identificativo);
            var results2 = await _repositoryNoTranslation.ToListAsync();
            Assert.Equal(5, results2.Count);
            var results3 = await _repositoryNoTranslation
                .OrderBy(x => x.Identificativo)
                .PageAsync(1, 2);
            Assert.Equal(2, results3.Items.Count);
            Assert.Equal(5, results3.TotalCount);
            Assert.Equal(1, results3.Items.First().Value!.Identificativo);
        }
    }
}
