using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.InMemory;
using RepositoryFramework.Migration;
using RepositoryFramework.UnitTest.Migration.Models;
using Xunit;

namespace RepositoryFramework.UnitTest.Migration
{
    public class MigrationTest
    {
        private static readonly IServiceProvider? s_serviceProvider;
        static MigrationTest()
        {
            DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration)
                .AddRepository<SuperMigrationUser, string>(builder =>
                {
                    builder.WithInMemory(name: "source").PopulateWithRandomData();
                })
                .AddRepository<SuperMigrationUser, string>(builder =>
                {
                    builder.WithInMemory(name: "target").PopulateWithRandomData();
                })
                    .AddMigrationManager<SuperMigrationUser, string>(settings =>
                    {
                        settings.SourceFactoryName = "source";
                        settings.DestinationFactoryName = "target";
                    })
                .Finalize(out s_serviceProvider)
                .WarmUpAsync()
                .ToResult();
        }
        private readonly IMigrationManager<SuperMigrationUser, string> _migrationService;
        private readonly IRepository<SuperMigrationUser, string> _repository;
        private readonly IRepository<SuperMigrationUser, string> _from;

        public MigrationTest()
        {
            _migrationService = s_serviceProvider!.GetService<IMigrationManager<SuperMigrationUser, string>>()!;
            var factory = s_serviceProvider!.GetService<IFactory<IRepository<SuperMigrationUser, string>>>()!;
            _from = factory.Create("source");
            _repository = factory.Create("target");
        }
        [Fact]
        public async Task TestAsync()
        {
            var migrationResult = await _migrationService.MigrateAsync(x => x.Id!, true).NoContext();
            Assert.Equal(4, (await _repository.QueryAsync().ToListAsync().NoContext()).Count);
            await foreach (var user in _from.QueryAsync())
            {
                Assert.True((await _repository.ExistAsync(user.Key!).NoContext()).IsOk);
                var newUser = await _repository.GetAsync(user.Key!).NoContext();
                Assert.NotNull(newUser);
                Assert.True(newUser!.IsAdmin == user.Value!.IsAdmin);
                Assert.True(newUser!.Email == user.Value!.Email);
                Assert.True(newUser!.Name == user.Value!.Name);
            }
            Assert.True(migrationResult);
        }
    }
}
