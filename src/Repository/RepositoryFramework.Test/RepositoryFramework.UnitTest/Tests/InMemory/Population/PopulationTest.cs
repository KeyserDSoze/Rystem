using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.InMemory;
using RepositoryFramework.UnitTest.InMemory.Population.Models;
using Xunit;

namespace RepositoryFramework.UnitTest.InMemory.Population
{
    public class PopulationTest
    {
        private static readonly IServiceProvider s_serviceProvider;
        static PopulationTest()
        {
            DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration)
                 .AddRepository<User, string>(settings =>
                 {
                     settings.WithInMemory(options =>
                     {
                         var writingRange = new Range(int.Parse(configuration["data_creation:delay_in_write_from"]),
                             int.Parse(configuration["data_creation:delay_in_write_to"]));
                         options.AddForCommandPattern(new MethodBehaviorSetting
                         {
                             MillisecondsOfWait = writingRange,
                         });
                         var readingRange = new Range(int.Parse(configuration["data_creation:delay_in_read_from"]),
                             int.Parse(configuration["data_creation:delay_in_read_to"]));
                         options.AddForQueryPattern(new MethodBehaviorSetting
                         {
                             MillisecondsOfWait = readingRange
                         });
                     })
                    .PopulateWithRandomData(100)
                    .WithPattern(x => x.Value.Email, @"[a-z]{4,10}@gmail\.com");
                 })
                .AddRepository<SuperUser, string>(settings =>
                {
                    settings.WithInMemory(options =>
                    {
                        var writingRange = new Range(int.Parse(configuration["data_creation:delay_in_write_from"]),
                            int.Parse(configuration["data_creation:delay_in_write_to"]));
                        options.AddForCommandPattern(new MethodBehaviorSetting
                        {
                            MillisecondsOfWait = writingRange,
                        });
                        var readingRange = new Range(int.Parse(configuration["data_creation:delay_in_read_from"]),
                            int.Parse(configuration["data_creation:delay_in_read_to"]));
                        options.AddForQueryPattern(new MethodBehaviorSetting
                        {
                            MillisecondsOfWait = readingRange
                        });
                    })
                    .PopulateWithRandomData(100)
                    .WithPattern(x => x.Value.Email, @"[a-z]{4,10}@gmail\.com");
                })
                .Finalize(out s_serviceProvider)
                .WarmUpAsync()
                .ToResult();
        }
        private readonly IRepository<User, string> _user1;
        private readonly IRepository<SuperUser, string> _user2;
        public PopulationTest()
        {
            _user1 = s_serviceProvider.GetService<IRepository<User, string>>()!;
            _user2 = s_serviceProvider.GetService<IRepository<SuperUser, string>>()!;
        }
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task TestAsync(int numberOfParameters)
        {
            switch (numberOfParameters)
            {
                case 1:
                    var users = await _user1.QueryAsync().ToListAsync().NoContext();
                    Assert.Equal(100, users.Count);
                    break;
                case 2:
                    var users2 = await _user2.QueryAsync().ToListAsync().NoContext();
                    Assert.Equal(100, users2.Count);
                    break;
            }
        }
    }
}
