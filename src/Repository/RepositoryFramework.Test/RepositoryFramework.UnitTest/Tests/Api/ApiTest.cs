using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RepositoryFramework.InMemory;
using RepositoryFramework.Test.Domain;
using RepositoryFramework.Test.Infrastructure.EntityFramework;
using RepositoryFramework.Test.Infrastructure.EntityFramework.Models.Internal;
using RepositoryFramework.Test.Models;
using RepositoryFramework.Test.Repository;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer("RepositoryFramework.UnitTest.PriorityOrderer", "RepositoryFramework.UnitTest")]

namespace RepositoryFramework.UnitTest.Tests.Api
{

    public class ApiTest
    {
        private const string Version = "v2";
        private const string Path = "SuperApi";
        private async Task<IServiceProvider> CreateHostServerAsync()
        {
            if (HttpClientFactory.Instance.Host == null)
            {
                var iAmWaiting = true;
                Exception? exception = null;
                var services = DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration);
                HttpClientFactory.Instance.Host = new HostBuilder()
                    .ConfigureWebHost(webHostBuilder =>
                        {
                            webHostBuilder
                            .UseTestServer()
                            .Configure(async app =>
                            {
                                await Task.CompletedTask;
                                try
                                {
                                    app.UseRouting();
                                    app.ApplicationServices.WarmUpAsync().ToResult();
                                    app.UseEndpoints(endpoints =>
                                    {
                                        endpoints.MapHealthChecks("/healthz");
                                        endpoints
                                            .UseApiFromRepositoryFramework()
                                            .WithNoAuthorization();
                                    });
                                }
                                catch (Exception ex)
                                {
                                    exception = ex;
                                }
                                iAmWaiting = false;
                            })
                            .ConfigureServices(services =>
                            {
                                services.AddHealthChecks();
                                services.AddControllers();
                                services.AddRepository<IperUser, string>(settings =>
                                {
                                    settings
                                        .WithInMemory()
                                        .PopulateWithRandomData(120, 5)
                                        .WithPattern(x => x.Value.Email, @"[a-z]{5,10}@gmail\.com");
                                    settings
                                        .AddBusiness()
                                            .AddBusinessBeforeInsert<IperRepositoryBeforeInsertBusiness>();
                                    settings
                                        .Translate<IperUser>();
                                });
                                services
                                    .AddRepository<Animal, AnimalKey>(settings =>
                                    {
                                        settings
                                            .WithInMemory();
                                        settings
                                            .AddBusiness()
                                                .AddBusinessBeforeInsert<AnimalBusinessBeforeInsert>()
                                                .AddBusinessBeforeInsert<AnimalBusinessBeforeInsert2>();
                                    });
                                services
                                    .AddRepository<Plant, int>(settings =>
                                    {
                                        settings
                                            .WithInMemory();
                                        settings
                                            .WithInMemoryCache(x =>
                                            {
                                                x.ExpiringTime = TimeSpan.FromSeconds(1);
                                                x.Methods = RepositoryMethods.All;
                                            });
                                    });

                                services
                                    .AddRepository<ExtremelyRareUser, string, ExtremelyRareUserRepositoryStorage>();
                                services
                                    .AddRepository<Car, Guid>(settings =>
                                    {
                                        settings.WithBlobStorage(x => x.ConnectionString = configuration["ConnectionString:Storage"]);
                                    });
                                services
                                    .AddBusinessForRepository<Car, Guid>()
                                        .AddBusinessBeforeInsert<CarBeforeInsertBusiness>()
                                        .AddBusinessBeforeInsert<CarBeforeInsertBusiness2>();
                                services
                                    .AddRepository<SuperCar, Guid>(
                                    settings =>
                                    {
                                        settings
                                            .WithTableStorage(x => x.ConnectionString = configuration["ConnectionString:Storage"])
                                            .WithPartitionKey(x => x.Id, x => x)
                                            .WithRowKey(x => x.Name)
                                            .WithTimestamp(x => x.Time)
                                            .WithTableStorageKeyReader<Car2KeyStorageReader>();
                                        settings
                                        .AddBusiness()
                                        .AddBusinessBeforeInsert<SuperCarBeforeInsertBusiness>()
                                        .AddBusinessBeforeInsert<SuperCarBeforeInsertBusiness2>();
                                    });
                                services
                                    .AddRepository<SuperUser, string>(
                                    settings =>
                                    {
                                        settings.WithCosmosSql(x =>
                                        {
                                            x.ConnectionString = configuration["ConnectionString:CosmosSql"];
                                            x.DatabaseName = "BigDatabase";
                                        })
                                            .WithId(x => x.Email!);
                                        settings
                                        .AddBusiness()
                                        .AddBusinessBeforeInsert<SuperUserBeforeInsertBusiness>()
                                        .AddBusinessBeforeInsert<SuperUserBeforeInsertBusiness2>();
                                    });
                                services
                                    .AddUserRepositoryWithDatabaseSqlAndEntityFramework(configuration);
                                services.
                                    AddRepository<CalamityUniverseUser, string>(settings =>
                                    {
                                        settings.WithDataverse(x =>
                                        {
                                            x.Prefix = "repo_";
                                            x.SolutionName = "TestAlessandro";
                                            if (configuration != null)
                                                x.SetConnection(configuration["ConnectionString:Dataverse:Environment"],
                                                    new(configuration["ConnectionString:Dataverse:ClientId"],
                                                 configuration["ConnectionString:Dataverse:ClientSecret"]));
                                        });
                                        settings
                                            .AddBusiness()
                                            .AddBusinessBeforeInsert<CalamityUniverseUserBeforeInsertBusiness>()
                                            .AddBusinessBeforeInsert<CalamityUniverseUserBeforeInsertBusiness2>();
                                    });
                                services.
                                   AddRepository<Cat, Guid>(settings =>
                                   {
                                       settings
                                        .WithMsSql(x =>
                                        {
                                            x.Schema = "repo";
                                            x.ConnectionString = configuration["ConnectionString:Database"];
                                        })
                                            .WithPrimaryKey(x => x.Id, x =>
                                            {
                                                x.ColumnName = "Key";
                                            })
                                           .WithColumn(x => x.Paws, x =>
                                           {
                                               x.ColumnName = "Zampe";
                                               x.IsNullable = true;
                                           });
                                   });
                                services.
                                  AddBusinessForRepository<Cat, Guid>()
                                    .AddBusinessBeforeInsert<CatBeforeInsertBusiness>()
                                    .AddBusinessBeforeInsert<CatBeforeInsertBusiness2>();
                                services.AddApiFromRepositoryFramework()
                                            .WithName<ExtremelyRareUser>("extremelyrareuserrefresh")
                                            .WithName<CalamityUniverseUser>("calamityuser")
                                            .WithDescriptiveName("Repository Api")
                                            .WithPath(Path)
                                            .WithSwagger()
                                            .WithVersion(Version)
                                            .WithDocumentation()
                                            .WithDefaultCors("http://example.com");
                            });
                        }).Build();
                await HttpClientFactory.Instance.Host!.StartAsync();
                while (iAmWaiting)
                {
                    await Task.Delay(100);
                }
                if (exception != null)
                    throw exception;
                var client = HttpClientFactory.Instance.CreateServerAndClient();

                var response = await client.GetAsync("/healthz");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/plain", response.Content.Headers.ContentType!.ToString());
                Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());

                try
                {
                    response = await client.GetAsync($"{Path}/{Version}/{nameof(ExtremelyRareUser)}/Delete?key=21");
                }
                catch (Exception ex)
                {
                    Assert.Equal("dasdsada", ex.Message);
                }
                response = await client.GetAsync($"{Path}/{Version}/extremelyrareuserrefresh/Get?key=21");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                response = await client.GetAsync($"{Path}/{Version}/extremelyrareuserrefresh/Exist?key=21");
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                services.AddSingleton<IHttpClientFactory>(HttpClientFactory.Instance);
                services
                    .AddRepository<SuperUser, string>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    })
                    .AddRepository<AppUser, AppUserKey>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    })
                    .AddRepository<Plant, int>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    })
                    .AddRepository<IperUser, string>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    })
                    .AddRepository<Animal, AnimalKey>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    })
                    .AddRepository<Car, Guid>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    })
                    .AddRepository<SuperCar, Guid>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    })
                    .AddRepository<CalamityUniverseUser, string>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithName("calamityuser")
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    })
                    .AddRepository<Cat, Guid>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    })
                    .AddRepository<MappingUser, int>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    })
                    .AddRepository<User, int>(settings =>
                    {
                        settings.WithApiClient(serviceLifetime: ServiceLifetime.Scoped)
                        .WithVersion(Version)
                        .WithStartingPath(Path);
                    });
                services.Finalize(out var serviceProvider);
                HttpClientFactory.Instance.ServiceProvider = serviceProvider;
            }
            return HttpClientFactory.Instance.ServiceProvider!;
        }
        [Fact, Priority(1)]
        public async Task InMemoryWithComplexKeyAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var repository = serviceProvider.GetService<IRepository<Animal, AnimalKey>>()!;
            var id = new AnimalKey(Guid.NewGuid().ToString(), 2, Guid.NewGuid());
            var entity = new Animal { Id = id, Name = "Horse" };
            var idNoInsert = new AnimalKey(Guid.NewGuid().ToString(), 4, Guid.NewGuid());
            var entityNoInsert = new Animal { Id = idNoInsert, Name = "Mouse", Paws = 120 };
            List<Entity<Animal, AnimalKey>> entities = new();
            for (var i = 0; i < 10; i++)
            {
                var batchId = new AnimalKey(Guid.NewGuid().ToString(), i, Guid.NewGuid());
                entities.Add(new Entity<Animal, AnimalKey>(new Animal { Id = batchId, Name = "Horse", Paws = i }, batchId));
            }
            await TestRepositoryAsync(repository!, id, entity,
                idNoInsert,
                entityNoInsert,
                entities,
                x => x.Id,
                x => x.Name == "Horse",
                x => x.Name != "Horse",
                x => x.Paws,
                (x, y) => x.Paws > y.Paws,
                entities.Max(x => x.Value!.Paws),
                entities.Min(x => x.Value!.Paws),
                (int)entities.Average(x => x.Value!.Paws),
                entities.Sum(x => x.Value!.Paws));
        }
        [Fact, Priority(2)]
        public async Task InMemoryAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var repository = serviceProvider.GetService<IRepository<IperUser, string>>()!;
            var id = Guid.NewGuid().ToString();
            var entity = new IperUser { Id = id, GroupId = Guid.NewGuid(), IsAdmin = true, Email = "alekud@drasda.it", Name = "Alekud", Port = 23 };
            var idNoInsert = Guid.NewGuid().ToString();
            var entityNoInsert = new IperUser { Id = id, GroupId = Guid.NewGuid(), IsAdmin = true, Email = "alekud@drasda.it", Name = "Alekud", Port = 120 };
            List<Entity<IperUser, string>> entities = new();
            for (var i = 0; i < 10; i++)
            {
                var batchId = Guid.NewGuid().ToString();
                entities.Add(new Entity<IperUser, string>(new IperUser { Id = id, GroupId = Guid.NewGuid(), IsAdmin = true, Email = "alekud@drasda.it", Name = "Alekud", Port = i }, batchId));
            }
            await TestRepositoryAsync(repository!, id, entity,
                idNoInsert,
                entityNoInsert,
                entities,
                x => x.Id,
                x => x.Name.Contains("eku"),
                x => !x.Name.Contains("eku"),
                x => x.Port,
                (x, y) => x.Port > y.Port,
                entities.Max(x => x.Value!.Port),
                entities.Min(x => x.Value!.Port),
                (int)entities.Average(x => x.Value!.Port),
                entities.Sum(x => x.Value!.Port));
        }
        [Fact, Priority(3)]
        public async Task SqlWithEntityFrameworkAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var repository = serviceProvider.GetService<IRepository<AppUser, AppUserKey>>()!;
            var id = new AppUserKey(23);
            var entity = new AppUser(23, "alekud", "alekud@drasda.it", new(), DateTime.UtcNow);
            var idNoInsert = new AppUserKey(120);
            var entityNoInsert = new AppUser(120, "alekud", "alekud@drasda.it", new(), DateTime.UtcNow);
            List<Entity<AppUser, AppUserKey>> entities = new();
            for (var i = 2; i <= 11; i++)
            {
                var batchId = new AppUserKey(i);
                entities.Add(new Entity<AppUser, AppUserKey>(new AppUser(i, "alekud", "alekud@drasda.it", new(), DateTime.UtcNow), batchId));
            }
            await TestRepositoryAsync(repository!, id, entity,
                idNoInsert,
                entityNoInsert,
                entities,
                x => x.Id,
                x => x.Username.Contains("eku"),
                x => !x.Username.Contains("eku"),
                x => x.Id,
                (x, y) => x.Id > y.Id,
                entities.Max(x => x.Value!.Id),
                entities.Min(x => x.Value!.Id),
                (int)entities.Average(x => x.Value!.Id),
                entities.Sum(x => x.Value!.Id));
        }
        [Fact, Priority(4)]
        public async Task TableStorageAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var repository = serviceProvider.GetService<IRepository<SuperCar, Guid>>()!;
            var id = Guid.NewGuid();
            var entity = new SuperCar() { Name = "name", Id = id, Other = "daa", Time = DateTime.UtcNow, Wheels = 2 };
            var idNoInsert = Guid.NewGuid();
            var entityNoInsert = new SuperCar() { Name = "name", Id = idNoInsert, Other = "daa", Time = DateTime.UtcNow, Wheels = 120 };
            List<Entity<SuperCar, Guid>> entities = new();
            for (var i = 0; i < 10; i++)
            {
                var batchId = Guid.NewGuid();
                entities.Add(new Entity<SuperCar, Guid>(new SuperCar { Id = batchId, Name = "name", Wheels = i }, batchId));
            }
            await TestRepositoryAsync(repository!, id, entity,
                idNoInsert,
                entityNoInsert,
                entities,
                x => x.Id,
                x => x.Name == "name",
                x => x.Name != "name",
                x => x.Wheels,
                (x, y) => x.Wheels > y.Wheels,
                entities.Max(x => x.Value!.Wheels),
                entities.Min(x => x.Value!.Wheels),
                (int)entities.Average(x => x.Value!.Wheels),
                entities.Sum(x => x.Value!.Wheels));
        }
        [Fact, Priority(5)]
        public async Task BlobStorageAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var repository = serviceProvider.GetService<IRepository<Car, Guid>>()!;
            var id = Guid.NewGuid();
            var entity = new Car() { Name = "name", Id = id, Wheels = 2 };
            var idNoInsert = Guid.NewGuid();
            var entityNoInsert = new Car() { Name = "name", Id = idNoInsert, Wheels = 120 };
            List<Entity<Car, Guid>> entities = new();
            for (var i = 0; i < 10; i++)
            {
                var batchId = Guid.NewGuid();
                entities.Add(new Entity<Car, Guid>(new Car { Id = batchId, Name = "name", Wheels = i }, batchId));
            }
            await TestRepositoryAsync(repository!, id, entity,
                idNoInsert,
                entityNoInsert,
                entities,
                x => x.Id,
                x => x.Name == "name",
                x => x.Name != "name",
                x => x.Wheels,
                (x, y) => x.Wheels > y.Wheels,
                entities.Max(x => x.Value!.Wheels),
                entities.Min(x => x.Value!.Wheels),
                (int)entities.Average(x => x.Value!.Wheels),
                entities.Sum(x => x.Value!.Wheels));
        }
        [Fact, Priority(6)]
        public async Task CosmosSqlAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var userRepository = serviceProvider.GetService<IRepository<SuperUser, string>>()!;
            var id = "dasdasdsa@gmail.com";
            var entity = new SuperUser(id);
            var idNoInsert = "dasdasdsa120@gmail.com";
            var entityNoInsert = new SuperUser(idNoInsert) { Port = 120 };
            List<Entity<SuperUser, string>> entities = new();
            for (var i = 0; i < 10; i++)
            {
                var batchId = $"dasdasdsa{i}@gmail.com";
                entities.Add(new Entity<SuperUser, string>(new SuperUser(batchId) { Port = i }, batchId));
            }
            await TestRepositoryAsync(userRepository!, id, entity,
                idNoInsert,
                entityNoInsert,
                entities,
                x => x.Email!,
                x => x.Email!.Contains("sda"),
                x => x.Email!.Contains("ads"),
                x => x.Port,
                (x, y) => x.Port > y.Port,
                entities.Max(x => x.Value!.Port),
                entities.Min(x => x.Value!.Port),
                (int)entities.Average(x => x.Value!.Port),
                entities.Sum(x => x.Value!.Port));
        }
        [Fact, Priority(7)]
        public async Task DataverseAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var repository = serviceProvider.GetService<IRepository<CalamityUniverseUser, string>>()!;
            var id = "dasdasdsa@gmail.com";
            var entity = new CalamityUniverseUser { Email = id };
            var idNoInsert = "dasdasdsa120@gmail.com";
            var entityNoInsert = new CalamityUniverseUser { Email = idNoInsert, Port = 120 };
            List<Entity<CalamityUniverseUser, string>> entities = new();
            for (var i = 0; i < 10; i++)
            {
                var batchId = $"dasdasdsa{i}@gmail.com";
                entities.Add(new Entity<CalamityUniverseUser, string>(new CalamityUniverseUser { Email = batchId, Port = i }, batchId));
            }
            await TestRepositoryAsync(repository!, id, entity,
                idNoInsert,
                entityNoInsert,
                entities,
                x => x.Email!,
                x => x.Email!.Contains("sda"),
                x => x.Email!.Contains("ads"),
                x => x.Port,
                (x, y) => x.Port > y.Port,
                entities.Max(x => x.Value!.Port),
                entities.Min(x => x.Value!.Port),
                (int)entities.Average(x => x.Value!.Port),
                entities.Sum(x => x.Value!.Port));
        }
        [Fact, Priority(8)]
        public async Task MsSqlAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var repository = serviceProvider.GetService<IRepository<Cat, Guid>>()!;
            var id = Guid.NewGuid();
            var entity = new Cat() { Name = "name", Id = id, Paws = 2, Rooms = new List<Room> { new Room { IsSpecial = true, Name = "Solution" } } };
            var idNoInsert = Guid.NewGuid();
            var entityNoInsert = new Cat() { Name = "name", Id = idNoInsert, Paws = 120, Rooms = new List<Room> { new Room { IsSpecial = true, Name = "Solution" } } };
            List<Entity<Cat, Guid>> entities = new();
            for (var i = 0; i < 10; i++)
            {
                var batchId = Guid.NewGuid();
                entities.Add(new Entity<Cat, Guid>(new Cat { Id = batchId, Name = "name", Paws = i, Rooms = new List<Room> { new Room { IsSpecial = i % 2 == 0, Name = "Solution" } } }, batchId));
            }
            await TestRepositoryAsync(repository!, id, entity,
                idNoInsert,
                entityNoInsert,
                entities,
                x => x.Id,
                x => x.Name == "name",
                x => x.Name != "name",
                x => x.Paws,
                (x, y) => x.Paws > y.Paws,
                entities.Max(x => x.Value!.Paws),
                entities.Min(x => x.Value!.Paws),
                (int)entities.Average(x => x.Value!.Paws),
                entities.Sum(x => x.Value!.Paws));
        }
        [Fact, Priority(9)]
        public async Task EntityFrameworkAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var repository = serviceProvider.GetService<IRepository<MappingUser, int>>()!;
            var id = 23;
            var entity = new MappingUser(23, "alekud", "alekud@drasda.it", new(), DateTime.UtcNow);
            var idNoInsert = 120;
            var entityNoInsert = new MappingUser(120, "alekud", "alekud@drasda.it", new(), DateTime.UtcNow);
            List<Entity<MappingUser, int>> entities = new();
            for (var i = 2; i <= 11; i++)
            {
                var batchId = i;
                entities.Add(new Entity<MappingUser, int>(new MappingUser(i, "alekud", "alekud@drasda.it", new(), DateTime.UtcNow), batchId));
            }
            await TestRepositoryAsync(repository!, id, entity,
                idNoInsert,
                entityNoInsert,
                entities,
                x => x.Id,
                x => x.Username.Contains("eku"),
                x => !x.Username.Contains("eku"),
                x => x.Id,
                (x, y) => x.Id > y.Id,
                entities.Max(x => x.Value!.Id),
                entities.Min(x => x.Value!.Id),
                (int)entities.Average(x => x.Value!.Id),
                entities.Sum(x => x.Value!.Id));
        }
        [Fact, Priority(10)]
        public async Task EntityFrameworkSameModelAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var repository = serviceProvider.GetService<IRepository<User, int>>()!;
            var id = 23;
            var entity = new User
            {
                Identificativo = 23,
                Nome = "alekud",
                Cognome = "",
                IndirizzoElettronico = "alekud@drasda.it",
            };
            var idNoInsert = 120;
            var entityNoInsert = new User
            {
                Identificativo = idNoInsert,
                Nome = "alekud",
                Cognome = "",
                IndirizzoElettronico = "alekud@drasda.it",
            };
            List<Entity<User, int>> entities = new();
            for (var i = 2; i <= 11; i++)
            {
                var batchId = i;
                entities.Add(new Entity<User, int>(
                     new User
                     {
                         Identificativo = batchId,
                         Nome = "alekud",
                         Cognome = "",
                         IndirizzoElettronico = "alekud@drasda.it",
                     }, batchId));
            }
            await TestRepositoryAsync(repository!, id, entity,
                idNoInsert,
                entityNoInsert,
                entities,
                x => x.Identificativo,
                x => x.Nome.Contains("eku"),
                x => !x.Nome.Contains("eku"),
                x => x.Identificativo,
                (x, y) => x.Identificativo > y.Identificativo,
                entities.Max(x => x.Value!.Identificativo),
                entities.Min(x => x.Value!.Identificativo),
                (int)entities.Average(x => x.Value!.Identificativo),
                entities.Sum(x => x.Value!.Identificativo));
        }
        [Fact, Priority(11)]
        public async Task InMemoryWithCacheAsync()
        {
            var serviceProvider = (await CreateHostServerAsync()).CreateScope().ServiceProvider;
            var repository = serviceProvider.GetService<IRepository<Plant, int>>()!;
            var id = 32;
            var entity = new Plant { Id = id, IsATree = true, Name = "Alekud" };
            await TestRepositoryWithCacheAsync(repository!, id, entity,
                x => x.Id,
                x => x.Name.Contains("eku"),
                x => !x.Name.Contains("eku"));
        }
        private static async Task TestRepositoryAsync<T, TKey>(
            IRepository<T, TKey> repository,
            TKey testKey,
            T testEntity,
            TKey keyNotInsertedForBusinessReason,
            T entityNotInsertedForBusinessReason,
            List<Entity<T, TKey>> elements,
            Func<T, TKey> keyRetriever,
            Expression<Func<T, bool>> ok,
            Expression<Func<T, bool>> ko,
            Expression<Func<T, int>> predicate,
            Func<T, T, bool> orderCheck,
            int maxValue, int minValue, int averageValue, int sumValue)
            where TKey : notnull
        {
            foreach (var deletable in await repository.ToListAsync())
                await repository.DeleteAsync(deletable.Key!);
            var hasUser = await repository.ExistAsync(testKey);
            Assert.False(hasUser);
            var users = await repository.ToListAsync();
            Assert.Empty(users);
            var addUser = await repository.InsertAsync(testKey, testEntity);
            Assert.True(addUser);
            hasUser = await repository.ExistAsync(testKey);
            Assert.True(hasUser);
            addUser = await repository.InsertAsync(keyNotInsertedForBusinessReason, entityNotInsertedForBusinessReason);
            Assert.False(addUser);
            Assert.Equal(100, addUser.Code!);
            users = await repository.ToListAsync();
            Assert.Single(users);
            var user = await repository.GetAsync(testKey);
            Assert.Equal(testKey, keyRetriever(user!));
            users = await repository.Where(ok).ToListAsync();
            Assert.Single(users);
            users = await repository.Where(ko).ToListAsync();
            Assert.Empty(users);
            var deleted = await repository.DeleteAsync(testKey);
            Assert.True(deleted);
            users = await repository.Where(ok).ToListAsync();
            Assert.Empty(users);
            users = await repository.ToListAsync();
            Assert.Empty(users);
            hasUser = await repository.ExistAsync(testKey);
            Assert.False(hasUser);
            user = await repository.GetAsync(testKey);
            Assert.Null(user);
            var batchOperation = repository.CreateBatchOperation();
            foreach (var element in elements)
                batchOperation.AddInsert(element.Key!, element.Value!);
            await batchOperation.ExecuteAsync();
            users = await repository.ToListAsync();
            Assert.Equal(elements.Count, users.Count);
            users = await repository.Where(ok).ToListAsync();
            var totalOkItems = users.Count;
            var page = await repository.Where(ok).OrderByDescending(predicate).PageAsync(1, 2);
            Assert.True(orderCheck(page.Items.First().Value!, page.Items.Last().Value!));
            Assert.Equal(2, page.Items.Count());
            Assert.Equal(totalOkItems, page.TotalCount);
            batchOperation = repository.CreateBatchOperation();
            foreach (var element in elements)
                batchOperation.AddUpdate(element.Key!, element.Value!);
            await batchOperation.ExecuteAsync();
            users = await repository.ToListAsync();
            Assert.Equal(elements.Count, users.Count);

            var max = await repository.MaxAsync(predicate);
            Assert.Equal(maxValue, max);
            var min = await repository.MinAsync(predicate);
            Assert.Equal(minValue, min);
            var sum = await repository.SumAsync(predicate);
            Assert.Equal(sumValue, sum);
            var average = await repository.AverageAsync(predicate);
            Assert.Equal(averageValue, average);

            batchOperation = repository.CreateBatchOperation();
            foreach (var element in elements)
                batchOperation.AddDelete(element.Key!);
            await batchOperation.ExecuteAsync();
            users = await repository.ToListAsync();
            Assert.Empty(users);
        }
        private static async Task TestRepositoryWithCacheAsync<T, TKey>(
            IRepository<T, TKey> repository,
            TKey testKey,
            T testEntity,
            Func<T, TKey> keyRetriever,
            Expression<Func<T, bool>> ok,
            Expression<Func<T, bool>> ko)
            where TKey : notnull
        {
            var hasUser = await repository.ExistAsync(testKey);
            Assert.False(hasUser);
            var users = await repository.ToListAsync();
            Assert.Empty(users);
            var addUser = await repository.InsertAsync(testKey, testEntity);
            Assert.True(addUser);
            hasUser = await repository.ExistAsync(testKey);
            Assert.True(hasUser);
            users = await repository.ToListAsync();
            Assert.Empty(users);
            await Task.Delay(1500);
            users = await repository.ToListAsync();
            Assert.Single(users);
            var user = await repository.GetAsync(testKey);
            Assert.Equal(testKey, keyRetriever(user!));
            users = await repository.Where(ok).ToListAsync();
            Assert.Single(users);
            users = await repository.Where(ko).ToListAsync();
            Assert.Empty(users);
            var deleted = await repository.DeleteAsync(testKey);
            Assert.True(deleted);
            users = await repository.Where(ok).ToListAsync();
            Assert.Single(users);
            await Task.Delay(1500);
            users = await repository.Where(ok).ToListAsync();
            Assert.Empty(users);
            users = await repository.ToListAsync();
            Assert.Empty(users);
            hasUser = await repository.ExistAsync(testKey);
            Assert.False(hasUser);
            user = await repository.GetAsync(testKey);
            Assert.Null(user);
        }
    }
}
