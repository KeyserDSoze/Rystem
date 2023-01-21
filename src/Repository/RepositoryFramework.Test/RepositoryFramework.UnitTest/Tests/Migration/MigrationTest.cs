using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.InMemory;
using RepositoryFramework.Migration;
using RepositoryFramework.UnitTest.Migration.Models;
using RepositoryFramework.UnitTest.Migration.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RepositoryFramework.UnitTest.Migration
{
    public class MigrationTest
    {
        private static readonly IServiceProvider? s_serviceProvider;
        static MigrationTest()
        {
            DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration)
                    .AddRepository<SuperMigrationUser, string, SuperMigrationTo>(settings =>
                    {
                        settings
                            .AddMigrationSource<SuperMigrationUser, string, SuperMigrationFrom>(x => x.NumberOfConcurrentInserts = 2);
                    })
                .Finalize(out s_serviceProvider)
                .WarmUpAsync()
                .ToResult();
        }
        private readonly IMigrationManager<SuperMigrationUser, string> _migrationService;
        private readonly IRepository<SuperMigrationUser, string> _repository;
        private readonly IMigrationSource<SuperMigrationUser, string> _from;

        public MigrationTest()
        {
            _migrationService = s_serviceProvider!.GetService<IMigrationManager<SuperMigrationUser, string>>()!;
            _repository = s_serviceProvider!.GetService<IRepository<SuperMigrationUser, string>>()!;
            _from = s_serviceProvider!.GetService<IMigrationSource<SuperMigrationUser, string>>()!;
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
