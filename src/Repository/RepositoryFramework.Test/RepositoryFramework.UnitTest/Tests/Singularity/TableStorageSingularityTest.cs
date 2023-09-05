using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RepositoryFramework.UnitTest.Tests.Singularity
{
    public class TableStorageSingularityTest
    {
        public sealed record SecurityOptions(string Name, RsaOptions Rsa);
        public sealed class RsaOptions
        {
            public byte[]? D { get; set; }
            public byte[]? DP { get; set; }
            public byte[]? DQ { get; set; }
            public byte[]? Exponent { get; set; }
            public byte[]? InverseQ { get; set; }
            public byte[]? Modulus { get; set; }
            public byte[]? P { get; set; }
            public byte[]? Q { get; set; }
            public DateTime ExpiringDate { get; set; }
            public RSAParameters ToParameters()
                => new()
                {
                    D = D,
                    DP = DP,
                    DQ = DQ,
                    Exponent = Exponent,
                    InverseQ = InverseQ,
                    Modulus = Modulus,
                    P = P,
                    Q = Q
                };
        }
        [Fact]
        public async Task RunBeforeWarmUpAsync()
        {
            const string tokenRepoKey = "token";
            var newServiceCollection = DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration);
            await newServiceCollection.AddRepositoryAsync<SecurityOptions, string>(async settings =>
            {
                await settings.WithTableStorageAsync(t =>
                {
                    t.Settings.ConnectionString = configuration["ConnectionString:Storage"];
                    t.WithPartitionKey(pk => pk.Name, x => x);
                });

                settings.WithInMemoryCache(c =>
                {
                    c.ExpiringTime = TimeSpan.FromDays(365);
                });
            });

            var options = (await newServiceCollection.ExecuteUntilNowWithWarmUpAsync(
                async (IRepository<SecurityOptions, string> rsaRepository) =>
                {
                    if (!await rsaRepository.ExistAsync(tokenRepoKey))
                    {
                        using var rsa = new RSACryptoServiceProvider(2048);
                        var rsaOptions = ToRsaOptions(rsa.ExportParameters(true));
                        await rsaRepository.InsertAsync(tokenRepoKey, new SecurityOptions(tokenRepoKey, rsaOptions));
                    }

                    var options = await rsaRepository.GetAsync(tokenRepoKey).NoContext();
                    return options;
                }).NoContext())!;

            newServiceCollection.AddSingleton(options);
            Assert.True(newServiceCollection.Count > 0);
        }
        public RsaOptions ToRsaOptions(RSAParameters parameters)
        => new()
        {
            D = parameters.D,
            DP = parameters.DP,
            DQ = parameters.DQ,
            Exponent = parameters.Exponent,
            InverseQ = parameters.InverseQ,
            Modulus = parameters.Modulus,
            P = parameters.P,
            Q = parameters.Q,
            ExpiringDate = DateTime.UtcNow.AddDays(7)
        };
    }
}
