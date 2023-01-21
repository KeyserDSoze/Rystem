using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.InMemory;
using RepositoryFramework.UnitTest.InMemory.RandomPopulation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace RepositoryFramework.UnitTest.InMemory.RandomPopulation
{
    //https://github.com/moodmosaic/Fare/tree/master/Src/Fare
    public class RandomCreationTest
    {
        private static readonly IServiceProvider s_serviceProvider;
        static RandomCreationTest()
        {
            DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration)
                  .AddRepository<PopulationTest, string>(settings =>
                  {
                      settings
                        .WithInMemory()
                          .PopulateWithRandomData()
                            .WithPattern(x => x.Value!.J!.First().A, "[a-z]{4,5}")
                            .WithPattern(x => x.Value!.Y!.First().Value.A, "[a-z]{4,5}")
                            .WithImplementation(x => x.Value!.I, typeof(MyInnerInterfaceImplementation))
                            .WithPattern(x => x.Value!.I!.A!, "[a-z]{4,5}")
                            .WithPattern(x => x.Value!.II!.A!, "[a-z]{4,5}")
                            .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.Value!.I!);
                  })
                .AddRepository<RegexPopulationTest, string>(settings =>
                {
                    settings
                        .WithInMemory()
                        .PopulateWithRandomData(90, 8)
                        .WithAutoIncrement(x => x.Value!.Id, 0)
                        .WithPattern(x => x.Value!.A, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.AA, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.B, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.BB, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.C, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.CC, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.D, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.DD, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.E, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.EE, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.F, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.FF, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.G, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.GG, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.H, "[1-9]{1,3}")
                        .WithPattern(x => x.Value!.HH, "[1-9]{1,3}")
                        .WithPattern(x => x.Value!.L, "[1-9]{1,3}")
                        .WithPattern(x => x.Value!.LL, "[1-9]{1,3}")
                        .WithPattern(x => x.Value!.M, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.MM, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.N, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.NN, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.O, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.OO, "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.P, "[1-9a-zA-Z]{5,20}")
                        .WithPattern(x => x.Value!.Q, "true")
                        .WithPattern(x => x.Value!.QQ, "true")
                        .WithPattern(x => x.Value!.R, "[a-z]{1}")
                        .WithPattern(x => x.Value!.RR, "[a-z]{1}")
                        .WithPattern(x => x.Value!.S, "([0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12})")
                        .WithPattern(x => x.Value!.SS, "([0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12})")
                        .WithPattern(x => x.Value!.T, @"(?:2018|2019|2020|2021|2022)/(?:10|11|12)/(?:06|07|08) (00:00:00)")
                        .WithPattern(x => x.Value!.TT, @"(?:2018|2019|2020|2021|2022)/(?:10|11|12)/(?:06|07|08) (00:00:00)")
                        .WithPattern(x => x.Value!.U, "[1-9]{1,4}")
                        .WithPattern(x => x.Value!.UU, "[1-9]{1,4}")
                        .WithPattern(x => x.Value!.V, @"(?:10|11|12)/(?:06|07|08)/(?:2018|2019|2020|2021|2022) (00:00:00 AM \+00:00)")
                        .WithPattern(x => x.Value!.VV, @"(?:10|11|12)/(?:06|07|08)/(?:2018|2019|2020|2021|2022) (00:00:00 AM \+00:00)")
                        .WithPattern(x => x.Value!.Z, "[1-9]{1,2}", "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.ZZ, "[1-9]{1,2}", "[1-9]{1,2}")
                        .WithPattern(x => x.Value!.J!.First().A, "[a-z]{4,5}")
                        .WithPattern(x => x.Value!.Y!.First().Value.A, "[a-z]{4,5}");
                })
                .AddQuery<DelegationPopulation, string>(settings =>
                {
                    settings
                        .WithInMemory()
                            .PopulateWithRandomData()
                            .WithValue(x => x.Value!.A, () => 2)
                            .WithValue(x => x.Value!.AA, () => 2)
                            .WithValue(x => x.Value!.B, () => (uint)2)
                            .WithValue(x => x.Value!.BB, () => (uint)2)
                            .WithValue(x => x.Value!.C, () => (byte)2)
                            .WithValue(x => x.Value!.CC, () => (byte)2)
                            .WithValue(x => x.Value!.D, () => (sbyte)2)
                            .WithValue(x => x.Value!.DD, () => (sbyte)2)
                            .WithValue(x => x.Value!.E, () => (short)2)
                            .WithValue(x => x.Value!.EE, () => (short)2)
                            .WithValue(x => x.Value!.F, () => (ushort)2)
                            .WithValue(x => x.Value!.FF, () => (ushort)2)
                            .WithValue(x => x.Value!.G, () => 2)
                            .WithValue(x => x.Value!.GG, () => 2)
                            .WithValue(x => x.Value!.H, () => (nint)2)
                            .WithValue(x => x.Value!.HH, () => (nint)2)
                            .WithValue(x => x.Value!.L, () => (nuint)2)
                            .WithValue(x => x.Value!.LL, () => (nuint)2)
                            .WithValue(x => x.Value!.M, () => 2)
                            .WithValue(x => x.Value!.MM, () => 2)
                            .WithValue(x => x.Value!.N, () => 2)
                            .WithValue(x => x.Value!.NN, () => 2)
                            .WithValue(x => x.Value!.O, () => 2)
                            .WithValue(x => x.Value!.OO, () => 2)
                            .WithValue(x => x.Value!.P, () => Guid.NewGuid().ToString())
                            .WithValue(x => x.Value!.Q, () => true)
                            .WithValue(x => x.Value!.QQ, () => true)
                            .WithValue(x => x.Value!.R, () => 'a')
                            .WithValue(x => x.Value!.RR, () => 'a')
                            .WithValue(x => x.Value!.S, () => Guid.NewGuid())
                            .WithValue(x => x.Value!.SS, () => Guid.NewGuid())
                            .WithValue(x => x.Value!.T, () => DateTime.UtcNow)
                            .WithValue(x => x.Value!.TT, () => DateTime.UtcNow)
                            .WithValue(x => x.Value!.U, () => new TimeSpan(2))
                            .WithValue(x => x.Value!.UU, () => new TimeSpan(2))
                            .WithValue(x => x.Value!.V, () => DateTimeOffset.UtcNow)
                            .WithValue(x => x.Value!.VV, () => DateTimeOffset.UtcNow)
                            .WithValue(x => x.Value!.Z, () => new Range(new Index(1), new Index(2)))
                            .WithValue(x => x.Value!.ZZ, () => new Range(new Index(1), new Index(2)))
                            .WithValue(x => x.Value!.J, () =>
                            {
                                List<InnerDelegationPopulation> inners = new();
                                for (var i = 0; i < 10; i++)
                                {
                                    inners.Add(new InnerDelegationPopulation()
                                    {
                                        A = i.ToString(),
                                        B = i
                                    });
                                }
                                return inners;
                            });
                })
                .AddRepository<AutoincrementModel, int>(settings =>
                {
                    settings
                        .WithInMemory()
                        .PopulateWithRandomData()
                        .WithAutoIncrement(x => x.Value!.Id, 0);
                })
                .AddRepository<AutoincrementModel2, int>(settings =>
                {
                    settings
                        .WithInMemory()
                        .PopulateWithRandomData()
                        .WithAutoIncrement(x => x.Value!.Id, 1);
                })
                .Finalize(out s_serviceProvider)
                .WarmUpAsync()
                .ToResult();
        }
        private readonly IRepository<PopulationTest, string> _test;
        private readonly IRepository<RegexPopulationTest, string> _population;
        private readonly IQuery<DelegationPopulation, string> _delegation;
        private readonly IRepository<AutoincrementModel, int> _autoincrementRepository;
        private readonly IRepository<AutoincrementModel2, int> _autoincrementRepository2;

        public RandomCreationTest()
        {
            _test = s_serviceProvider.GetService<IRepository<PopulationTest, string>>()!;
            _population = s_serviceProvider.GetService<IRepository<RegexPopulationTest, string>>()!;
            _delegation = s_serviceProvider.GetService<IQuery<DelegationPopulation, string>>()!;
            _autoincrementRepository = s_serviceProvider.GetService<IRepository<AutoincrementModel, int>>()!;
            _autoincrementRepository2 = s_serviceProvider.GetService<IRepository<AutoincrementModel2, int>>()!;
        }
        [Fact]
        public async Task TestWithoutRegexAsync()
        {
            var all = await _test.QueryAsync().ToListAsync().NoContext();
            var theFirst = all.First().Value!;
            Assert.NotEqual(0, theFirst.A);
            Assert.NotNull(theFirst.AA);
            Assert.NotEqual((uint)0, theFirst.B);
            Assert.NotNull(theFirst.BB);
            Assert.NotEqual((byte)0, theFirst.C);
            Assert.NotNull(theFirst.CC);
            Assert.NotEqual((sbyte)0, theFirst.D);
            Assert.NotNull(theFirst.DD);
            Assert.NotEqual((short)0, theFirst.E);
            Assert.NotNull(theFirst.EE);
            Assert.NotEqual((ushort)0, theFirst.F);
            Assert.NotNull(theFirst.FF);
            Assert.NotEqual((long)0, theFirst.G);
            Assert.NotNull(theFirst.GG);
            Assert.NotEqual((nint)0, theFirst.H);
            Assert.NotNull(theFirst.HH);
            Assert.NotEqual((nuint)0, theFirst.L);
            Assert.NotNull(theFirst.LL);
            Assert.NotEqual((float)0, theFirst.M);
            Assert.NotNull(theFirst.MM);
            Assert.NotEqual((double)0, theFirst.N);
            Assert.NotNull(theFirst.NN);
            Assert.NotEqual((decimal)0, theFirst.O);
            Assert.NotNull(theFirst.OO);
            Assert.NotNull(theFirst.P);
            Assert.NotNull(theFirst.PP);
            Assert.NotNull(theFirst.QQ);
            Assert.NotEqual((char)0, theFirst.R);
            Assert.NotNull(theFirst.RR);
            Assert.NotEqual(Guid.Empty, theFirst.S);
            Assert.NotNull(theFirst.SS);
            Assert.NotEqual(new DateTime(), theFirst.T);
            Assert.NotNull(theFirst.TT);
            Assert.NotEqual(TimeSpan.FromTicks(0), theFirst.U);
            Assert.NotNull(theFirst.UU);
            Assert.NotEqual(new DateTimeOffset(), theFirst.V);
            Assert.NotNull(theFirst.VV);
            Assert.NotEqual(new Range(), theFirst.Z);
            Assert.NotNull(theFirst.ZZ);
            Assert.NotNull(theFirst.X);
            Assert.Equal(10, theFirst?.X?.Count());
            Assert.NotNull(theFirst?.Y);
            Assert.Equal(10, theFirst?.Y?.Count);
            Assert.NotNull(theFirst?.W);
            Assert.Equal(10, theFirst?.W?.Length);
            Assert.NotNull(theFirst?.J);
            Assert.Equal(10, theFirst?.J?.Count);
            var regex = new Regex("[a-z]{4,5}");
            foreach (var check in theFirst!.J!)
            {
                Assert.Equal(check.A,
                    regex.Matches(check!.A!).OrderByDescending(x => x.Length).First().Value);
            }
            foreach (var check in theFirst!.Y!)
            {
                Assert.Equal(check.Value.A,
                    regex.Matches(check!.Value!.A!).OrderByDescending(x => x.Length).First().Value);
            }
            Assert.Equal(theFirst.I!.A!,
                    regex.Matches(theFirst.I!.A!).OrderByDescending(x => x.Length).First().Value);
            Assert.Equal(theFirst.II!.A!,
                    regex.Matches(theFirst.II!.A!).OrderByDescending(x => x.Length).First().Value);
        }
        [Fact]
        public async Task TestWithRegexAsync()
        {
            var all = await _population.OrderBy(x => x.Id).QueryAsync().ToListAsync().NoContext();
            var theFirst = all.First().Value!;
            Assert.Equal(90, all.Count);
            Assert.Equal(0, all.First().Value!.Id);
            Assert.Equal(89, all.Last().Value!.Id);
            Assert.NotEqual(0, theFirst.A);
            Assert.NotNull(theFirst.AA);
            Assert.NotEqual((uint)0, theFirst.B);
            Assert.NotNull(theFirst.BB);
            Assert.NotEqual((byte)0, theFirst.C);
            Assert.NotNull(theFirst.CC);
            Assert.NotEqual((sbyte)0, theFirst.D);
            Assert.NotNull(theFirst.DD);
            Assert.NotEqual((short)0, theFirst.E);
            Assert.NotNull(theFirst.EE);
            Assert.NotEqual((ushort)0, theFirst.F);
            Assert.NotNull(theFirst.FF);
            Assert.NotEqual((long)0, theFirst.G);
            Assert.NotNull(theFirst.GG);
            Assert.NotEqual((nint)0, theFirst.H);
            Assert.NotNull(theFirst.HH);
            Assert.NotEqual((nuint)0, theFirst.L);
            Assert.NotNull(theFirst.LL);
            Assert.NotEqual((float)0, theFirst.M);
            Assert.NotNull(theFirst.MM);
            Assert.NotEqual((double)0, theFirst.N);
            Assert.NotNull(theFirst.NN);
            Assert.NotEqual((decimal)0, theFirst.O);
            Assert.NotNull(theFirst.OO);
            Assert.NotNull(theFirst.P);
            Assert.NotNull(theFirst.PP);
            Assert.NotNull(theFirst.QQ);
            Assert.NotEqual((char)0, theFirst.R);
            Assert.NotNull(theFirst.RR);
            Assert.NotEqual(Guid.Empty, theFirst.S);
            Assert.NotNull(theFirst.SS);
            Assert.NotEqual(new DateTime(), theFirst.T);
            Assert.NotNull(theFirst.TT);
            Assert.NotEqual(TimeSpan.FromTicks(0), theFirst.U);
            Assert.NotNull(theFirst.UU);
            Assert.NotEqual(new DateTimeOffset(), theFirst.V);
            Assert.NotNull(theFirst.VV);
            Assert.NotEqual(new Range(), theFirst.Z);
            Assert.NotNull(theFirst.ZZ);
            Assert.NotNull(theFirst.X);
            Assert.Equal(8, theFirst?.X?.Count());
            Assert.NotNull(theFirst?.Y);
            Assert.Equal(8, theFirst?.Y?.Count);
            Assert.NotNull(theFirst?.W);
            Assert.Equal(8, theFirst?.W?.Length);
            Assert.NotNull(theFirst?.J);
            Assert.Equal(8, theFirst?.J?.Count);
            var regex = new Regex("[a-z]{4,5}");
            foreach (var check in theFirst!.J!)
            {
                Assert.Equal(check.A,
                    regex.Matches(check!.A!).OrderByDescending(x => x.Length).First().Value);
            }
            foreach (var check in theFirst!.Y!)
            {
                Assert.Equal(check.Value.A,
                    regex.Matches(check!.Value!.A!).OrderByDescending(x => x.Length).First().Value);
            }
        }
        [Fact]
        public async Task TestWithDelegationAsync()
        {
            var all = await _delegation.QueryAsync().ToListAsync().NoContext();
            var theFirst = all.First().Value!;
            Assert.Equal(2, theFirst.A);
            Assert.NotNull(theFirst.AA);
            Assert.Equal(2, theFirst.AA);
            Assert.Equal((uint)2, theFirst.B);
            Assert.NotNull(theFirst.BB);
            Assert.Equal((uint)2, theFirst.BB);
            Assert.Equal((byte)2, theFirst.C);
            Assert.NotNull(theFirst.CC);
            Assert.Equal((byte)2, theFirst.CC);
            Assert.Equal((sbyte)2, theFirst.D);
            Assert.NotNull(theFirst.DD);
            Assert.Equal((sbyte)2, theFirst.DD);
            Assert.Equal((short)2, theFirst.E);
            Assert.NotNull(theFirst.EE);
            Assert.Equal((short)2, theFirst.EE);
            Assert.Equal((ushort)2, theFirst.F);
            Assert.NotNull(theFirst.FF);
            Assert.Equal((ushort)2, theFirst.FF);
            Assert.Equal(2, theFirst.G);
            Assert.NotNull(theFirst.GG);
            Assert.Equal(2, theFirst.GG);
            Assert.Equal(2, theFirst.H);
            Assert.NotNull(theFirst.HH);
            Assert.Equal(2, theFirst.HH);
            Assert.Equal((nuint)2, theFirst.L);
            Assert.NotNull(theFirst.LL);
            Assert.Equal((nuint)2, theFirst.LL);
            Assert.Equal(2, theFirst.M);
            Assert.NotNull(theFirst.MM);
            Assert.Equal(2, theFirst.MM);
            Assert.Equal(2, theFirst.N);
            Assert.NotNull(theFirst.NN);
            Assert.Equal(2, theFirst.NN);
            Assert.Equal(2, theFirst.O);
            Assert.NotNull(theFirst.OO);
            Assert.Equal(2, theFirst.OO);
            Assert.NotNull(theFirst.P);
            Assert.NotNull(theFirst.PP);
            Assert.True(theFirst.Q);
            Assert.NotNull(theFirst.QQ);
            Assert.True(theFirst.QQ);
            Assert.Equal('a', theFirst.R);
            Assert.NotNull(theFirst.RR);
            Assert.Equal('a', theFirst.RR);
            Assert.NotEqual(Guid.Empty, theFirst.S);
            Assert.NotNull(theFirst.SS);
            Assert.NotEqual(new DateTime(), theFirst.T);
            Assert.NotNull(theFirst.TT);
            Assert.NotEqual(TimeSpan.FromTicks(0), theFirst.U);
            Assert.NotNull(theFirst.UU);
            Assert.NotEqual(new DateTimeOffset(), theFirst.V);
            Assert.NotNull(theFirst.VV);
            Assert.Equal(1, theFirst.Z.Start);
            Assert.Equal(2, theFirst.Z.End);
            Assert.NotNull(theFirst.ZZ);
            Assert.Equal(1, theFirst.ZZ?.Start);
            Assert.Equal(2, theFirst.ZZ?.End);
            Assert.NotNull(theFirst.X);
            Assert.Equal(10, theFirst?.X?.Count());
            Assert.NotNull(theFirst?.Y);
            Assert.Equal(10, theFirst?.Y?.Count);
            Assert.NotNull(theFirst?.W);
            Assert.Equal(10, theFirst?.W?.Length);
            Assert.NotNull(theFirst?.J);
            Assert.Equal(10, theFirst?.J?.Count);
            var counter = 0;
            foreach (var check in theFirst!.J!)
            {
                Assert.Equal(counter.ToString(), check.A);
                Assert.Equal(counter, check.B);
                counter++;
            }
        }
        [Fact]
        public async Task TestWithAutoincrementAsync()
        {
            var all = await _autoincrementRepository.OrderBy(x => x.Id).QueryAsync().ToListAsync().NoContext();
            var all2 = await _autoincrementRepository2.OrderBy(x => x.Id).QueryAsync().ToListAsync().NoContext();
            Assert.Equal(0, all.First().Value!.Id);
            Assert.Equal(99, all.Last().Value!.Id);
            Assert.Equal(1, all2.First().Value!.Id);
            Assert.Equal(100, all2.Last().Value!.Id);
        }
    }
}
