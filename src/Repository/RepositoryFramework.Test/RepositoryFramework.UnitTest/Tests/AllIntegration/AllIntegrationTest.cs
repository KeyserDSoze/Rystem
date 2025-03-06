using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.Infrastructure.Azure.Storage.Blob;
using RepositoryFramework.Infrastructure.Azure.Storage.Table;
using RepositoryFramework.Test.Domain;
using RepositoryFramework.UnitTest.Tests.AllIntegration.TableStorage;
using Xunit;

namespace RepositoryFramework.UnitTest.Repository
{
    public class AllIntegrationTest
    {
        private sealed class BlobStorageConnectionService : IConnectionService<BlobContainerClientWrapper>
        {
            private readonly IConfiguration _configuration;
            private static readonly ConcurrentDictionary<string, bool> s_creationCheck = [];

            public BlobStorageConnectionService(IConfiguration configuration)
            {
                _configuration = configuration;
            }
            public BlobContainerClientWrapper GetConnection(string entityName, string? factoryName = null)
            {
                var tenantId = string.Empty;
                var blobContainerClientWrapper = new BlobContainerClientWrapper
                {
                    Client = new BlobContainerClient(_configuration["ConnectionString:Storage"], entityName.ToLower())
                };
                if (!s_creationCheck.ContainsKey(tenantId))
                {
                    blobContainerClientWrapper.Client.CreateIfNotExists();
                    s_creationCheck.TryAdd(tenantId, true);
                }
                return blobContainerClientWrapper;
            }
        }
        private sealed class TableStorageConnectionService : IConnectionService<TableClientWrapper<AppUser, AppUserKey>>
        {
            private readonly IConfiguration _configuration;

            public TableStorageConnectionService(IConfiguration configuration)
            {
                _configuration = configuration;
            }
            public TableClientWrapper<AppUser, AppUserKey> GetConnection(string entityName, string? factoryName = null)
            {
                var serviceClient = new TableServiceClient(_configuration["ConnectionString:Storage"]);
                var tableClient = new TableClient(_configuration["ConnectionString:Storage"], entityName.ToLower());
                var tableStorageClientWrapper = new TableClientWrapper<AppUser, AppUserKey>
                {
                    Client = tableClient,
                    Settings = new TableStorageSettings<AppUser, AppUserKey>
                    {
                        PartitionKey = "Id",
                        RowKey = "Username",
                        Timestamp = "CreationTime",
                        PartitionKeyFromKeyFunction = x => x.Id.ToString(),
                        PartitionKeyFunction = x => x.Id.ToString(),
                        RowKeyFunction = x => x.Username,
                        TimestampFunction = x => x.CreationTime
                    }
                };
                return tableStorageClientWrapper;
            }
        }
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
                        .AddRepositoryAsync<AppUser, AppUserKey>(async settings =>
                        {
                            await settings
                            .WithTableStorageAsync(builder =>
                            {
                                builder.Settings.ConnectionString = configuration["ConnectionString:Storage"];
                                builder
                                .WithTableStorageKeyReader<TableStorageKeyReader>()
                                    .WithPartitionKey(x => x.Id, x => x.Id)
                                    .WithRowKey(x => x.Username)
                                    .WithTimestamp(x => x.CreationTime);
                            });
                        }).ToResult();
                    break;
                case "tablestorage2":
                    services
                        .AddRepository<AppUser, AppUserKey>(settings =>
                        {
                            settings
                                .WithTableStorage<AppUser, AppUserKey, TableStorageConnectionService, TableStorageKeyReader>();
                        });
                    break;
                case "blobstorage":
                    services
                        .AddRepositoryAsync<AppUser, AppUserKey>(async settings =>
                        {
                            await settings
                                .WithBlobStorageAsync(x => x.Settings.ConnectionString = configuration["ConnectionString:Storage"])
                                .NoContext();
                        }).ToResult();
                    break;
                case "blobstorage2":
                    services
                        .AddRepository<AppUser, AppUserKey>(settings =>
                        {
                            settings
                                .WithBlobStorage<AppUser, AppUserKey, BlobStorageConnectionService>();
                        });
                    break;
                case "cosmos":
                    services.AddRepositoryAsync<AppUser, AppUserKey>(async settings =>
                    {
                        await settings.WithCosmosSqlAsync(x =>
                        {
                            x.Settings.ConnectionString = configuration["ConnectionString:CosmosSql"];
                            x.Settings.DatabaseName = "unittestdatabase";
                            x.WithId(x => new AppUserKey(x.Id));
                        }).NoContext();
                    })
                        .ToResult();
                    break;
            }
            services.Finalize(out var serviceProvider);
            return serviceProvider.GetService<IRepository<AppUser, AppUserKey>>()!;
        }

        [Theory]
        [InlineData("entityframework")]
        [InlineData("tablestorage")]
        [InlineData("tablestorage2")]
        [InlineData("blobstorage")]
        [InlineData("blobstorage2")]
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
            await batchOperation.ExecuteAsync().ToListAsync();

            items = await repository.Where(x => x.Id > 0).ToListAsync();
            Assert.Equal(10, items.Count);

            Expression<Func<AppUser, object>> orderPredicate = x => x.Id;
            var page = await repository.Where(x => x.Id > 0).OrderByDescending(orderPredicate).PageAsync(1, 2);
            Assert.True(page.Items.First().Value!.Id > page.Items.Last().Value!.Id);

            batchOperation = repository.CreateBatchOperation();
            await foreach (var appUser in repository.QueryAsync())
                batchOperation.AddUpdate(new AppUserKey(appUser.Value!.Id), new AppUser(appUser.Value.Id, $"User Updated {appUser.Value.Id}", $"Email Updated {appUser.Value.Id}", new(), DateTime.UtcNow));
            await batchOperation.ExecuteAsync().ToListAsync();

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
            await batchOperation.ExecuteAsync().ToListAsync();

            items = await repository.Where(x => x.Id > 0).QueryAsync().ToListAsync();
            Assert.Empty(items);
        }
    }
}
