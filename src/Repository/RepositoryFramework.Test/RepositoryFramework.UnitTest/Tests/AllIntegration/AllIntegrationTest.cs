using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.Test.Domain;
using RepositoryFramework.UnitTest.Tests.AllIntegration.TableStorage;
using Xunit;

namespace RepositoryFramework.UnitTest.Repository
{
    public class AllIntegrationTest
    {
        private static IRepository<AppUser, AppUserKey> GetCorrectIntegration(string injectionedStorage)
        {
            var services = DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration);
            switch (injectionedStorage)
            {
                case "entityframework":
                    services
                        .AddUserRepositoryWithDatabaseSqlAndEntityFramework(configuration);
                    break;
                case "tablestorage":
                    services
                        .AddRepository<AppUser, AppUserKey>(settings =>
                        {
                            settings
                            .WithTableStorage(x => x.ConnectionString = configuration["ConnectionString:Storage"])
                            .WithTableStorageKeyReader<TableStorageKeyReader>()
                                .WithPartitionKey(x => x.Id, x => x.Id)
                                .WithRowKey(x => x.Username)
                                .WithTimestamp(x => x.CreationTime);
                        });
                    break;
                case "blobstorage":
                    services
                        .AddRepository<AppUser, AppUserKey>(settings => settings
                            .WithBlobStorage(x => x.ConnectionString = configuration["ConnectionString:Storage"]));
                    break;
                case "cosmos":
                    services.AddRepository<AppUser, AppUserKey>(settings =>
                    {
                        settings.WithCosmosSql(x =>
                        {
                            x.ConnectionString = configuration["ConnectionString:CosmosSql"];
                            x.DatabaseName = "unittestdatabase";
                        })
                        .WithId(x => new AppUserKey(x.Id));
                    });
                    break;
            }
            services.Finalize(out var serviceProvider);
            return serviceProvider.GetService<IRepository<AppUser, AppUserKey>>()!;
        }

        [Theory]
        [InlineData("entityframework")]
        [InlineData("tablestorage")]
        [InlineData("blobstorage")]
        [InlineData("cosmos")]
        public async Task AllCommandsAndQueryAsync(string whatKindOfStorage)
        {
            var repository = GetCorrectIntegration(whatKindOfStorage);
            foreach (var appUser in await repository.ToListAsync())
            {
                await repository.DeleteAsync(new AppUserKey(appUser.Value!.Id));
            }
            var user = new AppUser(3, "Arnold", "Arnold@gmail.com", new(), DateTime.UtcNow);
            var result = await repository.InsertAsync(new AppUserKey(3), user);
            Assert.True(result.IsOk);
            user = user with { Id = result.Entity!.Value!.Id };
            var key = await CheckAsync("Arnold");

            result = await repository.UpdateAsync(key, user with { Username = "Fish" });
            Assert.True(result.IsOk);
            await CheckAsync("Fish");

            async Task<AppUserKey> CheckAsync(string name)
            {
                var items = await repository.Where(x => x.Id > 0).QueryAsync().ToListAsync();
                Assert.Single(items);
                var actual = await repository.FirstOrDefaultAsync(x => x.Id > 0);
                Assert.NotNull(actual);
                var key = new AppUserKey(actual!.Value!.Id);
                var item = await repository.GetAsync(key);
                Assert.NotNull(item);
                Assert.Equal(name, item!.Username);
                var result = await repository.ExistAsync(key);
                Assert.True(result.IsOk);
                return key;
            }

            result = await repository.DeleteAsync(key);
            Assert.True(result.IsOk);

            result = await repository.ExistAsync(key);
            Assert.False(result.IsOk);

            var items = await repository.Where(x => x.Id > 0).ToListAsync();
            Assert.Empty(items);

            var batchOperation = repository.CreateBatchOperation();
            for (var i = 1; i <= 10; i++)
                batchOperation.AddInsert(new AppUserKey(i), new AppUser(i, $"User {i}", $"Email {i}", new(), DateTime.UtcNow));
            await batchOperation.ExecuteAsync();

            items = await repository.Where(x => x.Id > 0).ToListAsync();
            Assert.Equal(10, items.Count);

            Expression<Func<AppUser, object>> orderPredicate = x => x.Id;
            var page = await repository.Where(x => x.Id > 0).OrderByDescending(orderPredicate).PageAsync(1, 2);
            Assert.True(page.Items.First().Value!.Id > page.Items.Last().Value!.Id);

            batchOperation = repository.CreateBatchOperation();
            await foreach (var appUser in repository.QueryAsync())
                batchOperation.AddUpdate(new AppUserKey(appUser.Value!.Id), new AppUser(appUser.Value.Id, $"User Updated {appUser.Value.Id}", $"Email Updated {appUser.Value.Id}", new(), DateTime.UtcNow));
            await batchOperation.ExecuteAsync();

            items = await repository.Where(x => x.Id > 0).ToListAsync();
            Assert.Equal(10, items.Count);
            Assert.Equal($"Email Updated {items.First().Value!.Id}", items.First().Value!.Email);

            var max = await repository.MaxAsync(x => x.Id);
            var min = await repository.MinAsync(x => x.Id);
            var preSum = 0;
            for (var i = min; i <= max; i++)
                preSum += i;
            var preAverage = (int)((decimal)preSum / ((decimal)max + 1 - (decimal)min));
            var sum = await repository.SumAsync(x => x.Id);
            Assert.Equal(preSum, sum);
            var average = await repository.AverageAsync(x => x.Id);
            Assert.InRange(preAverage, average - 1, average + 1);

            batchOperation = repository.CreateBatchOperation();
            foreach (var appUser in await repository.QueryAsync().ToListAsync())
                batchOperation.AddDelete(new AppUserKey(appUser.Value!.Id));
            await batchOperation.ExecuteAsync();

            items = await repository.Where(x => x.Id > 0).QueryAsync().ToListAsync();
            Assert.Empty(items);
        }
    }
}
