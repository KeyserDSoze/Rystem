using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RepositoryFramework.UnitTest.Tests.Singularity
{
    public class BlobStorageSingularityTest
    {
       public sealed class Something { }
       public sealed class Something2 { }
        [Fact]
        public async Task RunBeforeWarmUpAsync()
        {
            var newServiceCollection = DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration);
            await newServiceCollection.AddRepositoryAsync<Something, string>(async settings =>
            {
                await settings.WithBlobStorageAsync(t =>
                {
                    t.Settings.ConnectionString = configuration["ConnectionString:Storage"];
                });
            });
            await newServiceCollection.AddRepositoryAsync<Something2, string>(async settings =>
            {
                await settings.WithBlobStorageAsync(t =>
                {
                    t.Settings.ConnectionString = configuration["ConnectionString:Storage"];
                });
            });

            Assert.True(newServiceCollection.Count > 0);
        }
    }
}
