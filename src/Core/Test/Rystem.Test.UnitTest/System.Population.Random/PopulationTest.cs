using System.Collections.Generic;
using System;
using System.Reflection;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Population.Random;
using System.Linq;
using System.Text.RegularExpressions;
using Rystem.Test.UnitTest.System.Population.Random.Models;

namespace Rystem.Test.UnitTest.Population
{
    public class PopulationTest
    {
        public class DelegationPopulation
        {
            public int Id { get; set; }
            public int A { get; set; }
            public int? AA { get; set; }
            public uint B { get; set; }
            public uint? BB { get; set; }
            public byte C { get; set; }
            public byte? CC { get; set; }
            public sbyte D { get; set; }
            public sbyte? DD { get; set; }
            public short E { get; set; }
            public short? EE { get; set; }
            public ushort F { get; set; }
            public ushort? FF { get; set; }
            public long G { get; set; }
            public long? GG { get; set; }
            public nint H { get; set; }
            public nint? HH { get; set; }
            public nuint L { get; set; }
            public nuint? LL { get; set; }
            public float M { get; set; }
            public float? MM { get; set; }
            public double N { get; set; }
            public double? NN { get; set; }
            public decimal O { get; set; }
            public decimal? OO { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public string P { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public string? PP { get; set; }
            public bool Q { get; set; }
            public bool? QQ { get; set; }
            public char R { get; set; }
            public char? RR { get; set; }
            public Guid S { get; set; }
            public Guid? SS { get; set; }
            public DateTime T { get; set; }
            public DateTime? TT { get; set; }
            public TimeSpan U { get; set; }
            public TimeSpan? UU { get; set; }
            public DateTimeOffset V { get; set; }
            public DateTimeOffset? VV { get; set; }
            public Range Z { get; set; }
            public Range? ZZ { get; set; }
            public IEnumerable<InnerDelegationPopulation>? X { get; set; }
            public IDictionary<string, InnerDelegationPopulation>? Y { get; set; }
            public InnerDelegationPopulation[]? W { get; set; }
            public ICollection<InnerDelegationPopulation>? J { get; set; }
        }
        public class InnerDelegationPopulation
        {
            public string? A { get; set; }
            public int? B { get; set; }
        }
        public class Entity<T, TKey>
        {
            public T Value { get; set; }
            public TKey Key { get; set; }
        }
        [Fact]
        public void RandomizeWithUserModel()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddPopulationService();
            services
                .AddPopulationSettings<Entity<AppUser, int>>()
                .WithRandomValue(x => x.Value.Groups, async serviceProvider =>
                {
                    return new List<System.Population.Random.Models.Group>
                    {
                        new System.Population.Random.Models.Group
                        {
                            Id = "2",
                            Name = "asd",
                        },
                        new System.Population.Random.Models.Group
                        {
                            Id = "3",
                            Name = "asd555",
                        }
                    };
                });
            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            var populatedModel = serviceProvider.GetService<IPopulation<Entity<AppUser, int>>>();
            var all = populatedModel.Populate(67, 2);
            var first = all.First().Value;
        }
        [Fact]
        public void RandomizeWithWrapper()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddPopulationService();
            services
                .AddPopulationSettings<Entity<PopulationModelTest, int>>()
                .WithAutoIncrement(x => x.Value.A, 1)
                .WithAutoIncrement(x => x.Key, 1)
                .WithPattern(x => x.Value.J!.First().A, "[a-z]{4,5}")
                .WithPattern(x => x.Value.Y!.First().Value.A, "[a-z]{4,5}")
                .WithImplementation(x => x.Value.I, typeof(MyInnerInterfaceImplementation))
                .WithPattern(x => x.Value.I!.A!, "[a-z]{4,5}")
                .WithPattern(x => x.Value.II!.A!, "[a-z]{4,5}")
                .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.Value.I!);
            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            var populatedModel = serviceProvider.GetService<IPopulation<Entity<PopulationModelTest, int>>>();
            var all = populatedModel.Populate();
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
        public void RandomizeWithDefault()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddPopulationService();
            services
                .AddPopulationSettings<PopulationModelTest>()
                .WithAutoIncrement(x => x.A, 1);
            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            var populatedModel = serviceProvider.GetService<IPopulation<PopulationModelTest>>();
            var allPrepopulation = populatedModel.Setup();
            List<PopulationModelTest> all = new();
            for (int i = 0; i < 3; i++)
                all.AddRange(allPrepopulation.Populate(50, 4));
            Assert.Equal(150, all.Count);
            Assert.Equal(150, all.Last().A);
        }
        [Fact]
        public void DoubleRandomize()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddPopulationService();
            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            var populatedModel = serviceProvider.GetService<IPopulation<PopulationModelTest>>();
            var allPrepopulation = populatedModel.Setup().WithAutoIncrement(x => x.A, 1);
            List<PopulationModelTest> all = new();
            for (int i = 0; i < 3; i++)
                all.AddRange(allPrepopulation.Populate(50, 4));
            Assert.Equal(150, all.Count);
            Assert.Equal(150, all.Last().A);
        }
        [Fact]
        public void Randomize()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddPopulationService();
            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            var populatedModel = serviceProvider.GetService<IPopulation<PopulationModelTest>>();
            var allPrepopulation = populatedModel!
                .Setup()
                .WithPattern(x => x.J!.First().A, "[a-z]{4,5}")
                    .WithPattern(x => x.Y!.First().Value.A, "[a-z]{4,5}")
                    .WithImplementation(x => x.I, typeof(MyInnerInterfaceImplementation))
                    .WithPattern(x => x.I!.A!, "[a-z]{4,5}")
                    .WithPattern(x => x.II!.A!, "[a-z]{4,5}")
                    .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.I!);
            var all = allPrepopulation.Populate();
            var theFirst = all.First();
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
        public void RandomizeWithPatterns()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddPopulationService();
            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            var populatedModel = serviceProvider.GetService<IPopulation<DelegationPopulation>>();
            var allPrepopulation = populatedModel!
                .Setup()
                        .WithAutoIncrement(x => x.Id, 0)
                        .WithPattern(x => x.A, "[1-9]{1,2}")
                        .WithPattern(x => x.AA, "[1-9]{1,2}")
                        .WithPattern(x => x.B, "[1-9]{1,2}")
                        .WithPattern(x => x.BB, "[1-9]{1,2}")
                        .WithPattern(x => x.C, "[1-9]{1,2}")
                        .WithPattern(x => x.CC, "[1-9]{1,2}")
                        .WithPattern(x => x.D, "[1-9]{1,2}")
                        .WithPattern(x => x.DD, "[1-9]{1,2}")
                        .WithPattern(x => x.E, "[1-9]{1,2}")
                        .WithPattern(x => x.EE, "[1-9]{1,2}")
                        .WithPattern(x => x.F, "[1-9]{1,2}")
                        .WithPattern(x => x.FF, "[1-9]{1,2}")
                        .WithPattern(x => x.G, "[1-9]{1,2}")
                        .WithPattern(x => x.GG, "[1-9]{1,2}")
                        .WithPattern(x => x.H, "[1-9]{1,3}")
                        .WithPattern(x => x.HH, "[1-9]{1,3}")
                        .WithPattern(x => x.L, "[1-9]{1,3}")
                        .WithPattern(x => x.LL, "[1-9]{1,3}")
                        .WithPattern(x => x.M, "[1-9]{1,2}")
                        .WithPattern(x => x.MM, "[1-9]{1,2}")
                        .WithPattern(x => x.N, "[1-9]{1,2}")
                        .WithPattern(x => x.NN, "[1-9]{1,2}")
                        .WithPattern(x => x.O, "[1-9]{1,2}")
                        .WithPattern(x => x.OO, "[1-9]{1,2}")
                        .WithPattern(x => x.P, "[1-9a-zA-Z]{5,20}")
                        .WithPattern(x => x.Q, "true")
                        .WithPattern(x => x.QQ, "true")
                        .WithPattern(x => x.R, "[a-z]{1}")
                        .WithPattern(x => x.RR, "[a-z]{1}")
                        .WithPattern(x => x.S, "([0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12})")
                        .WithPattern(x => x.SS, "([0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12})")
                        .WithPattern(x => x.T, @"(?:2018|2019|2020|2021|2022)/(?:10|11|12)/(?:06|07|08) (00:00:00)")
                        .WithPattern(x => x.TT, @"(?:2018|2019|2020|2021|2022)/(?:10|11|12)/(?:06|07|08) (00:00:00)")
                        .WithPattern(x => x.U, "[1-9]{1,4}")
                        .WithPattern(x => x.UU, "[1-9]{1,4}")
                        .WithPattern(x => x.V, @"(?:10|11|12)/(?:06|07|08)/(?:2018|2019|2020|2021|2022) (00:00:00 AM \+00:00)")
                        .WithPattern(x => x.VV, @"(?:10|11|12)/(?:06|07|08)/(?:2018|2019|2020|2021|2022) (00:00:00 AM \+00:00)")
                        .WithPattern(x => x.Z, "[1-9]{1,2}", "[1-9]{1,2}")
                        .WithPattern(x => x.ZZ, "[1-9]{1,2}", "[1-9]{1,2}")
                        .WithPattern(x => x.J!.First().A, "[a-z]{4,5}")
                        .WithPattern(x => x.Y!.First().Value.A, "[a-z]{4,5}");

            var all = allPrepopulation.Populate(90, 8);
            var theFirst = all.First()!;
            Assert.Equal(90, all.Count);
            Assert.Equal(0, all.First().Id);
            Assert.Equal(89, all.Last().Id);
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
        public void RandomizeWithDelegation()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddPopulationService();
            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            var populatedModel = serviceProvider.GetService<IPopulation<DelegationPopulation>>();
            var allPrepopulation = populatedModel!
                .Setup()
                .WithValue(x => x.A, () => 2)
                            .WithValue(x => x.AA, () => 2)
                            .WithValue(x => x.B, () => (uint)2)
                            .WithValue(x => x.BB, () => (uint)2)
                            .WithValue(x => x.C, () => (byte)2)
                            .WithValue(x => x.CC, () => (byte)2)
                            .WithValue(x => x.D, () => (sbyte)2)
                            .WithValue(x => x.DD, () => (sbyte)2)
                            .WithValue(x => x.E, () => (short)2)
                            .WithValue(x => x.EE, () => (short)2)
                            .WithValue(x => x.F, () => (ushort)2)
                            .WithValue(x => x.FF, () => (ushort)2)
                            .WithValue(x => x.G, () => 2)
                            .WithValue(x => x.GG, () => 2)
                            .WithValue(x => x.H, () => (nint)2)
                            .WithValue(x => x.HH, () => (nint)2)
                            .WithValue(x => x.L, () => (nuint)2)
                            .WithValue(x => x.LL, () => (nuint)2)
                            .WithValue(x => x.M, () => 2)
                            .WithValue(x => x.MM, () => 2)
                            .WithValue(x => x.N, () => 2)
                            .WithValue(x => x.NN, () => 2)
                            .WithValue(x => x.O, () => 2)
                            .WithValue(x => x.OO, () => 2)
                            .WithValue(x => x.P, () => Guid.NewGuid().ToString())
                            .WithValue(x => x.Q, () => true)
                            .WithValue(x => x.QQ, () => true)
                            .WithValue(x => x.R, () => 'a')
                            .WithValue(x => x.RR, () => 'a')
                            .WithValue(x => x.S, () => Guid.NewGuid())
                            .WithValue(x => x.SS, () => Guid.NewGuid())
                            .WithValue(x => x.T, () => DateTime.UtcNow)
                            .WithValue(x => x.TT, () => DateTime.UtcNow)
                            .WithValue(x => x.U, () => new TimeSpan(2))
                            .WithValue(x => x.UU, () => new TimeSpan(2))
                            .WithValue(x => x.V, () => DateTimeOffset.UtcNow)
                            .WithValue(x => x.VV, () => DateTimeOffset.UtcNow)
                            .WithValue(x => x.Z, () => new Range(new Index(1), new Index(2)))
                            .WithValue(x => x.ZZ, () => new Range(new Index(1), new Index(2)))
                            .WithValue(x => x.J, () =>
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
            var all = allPrepopulation.Populate();
            var theFirst = all.First();
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
    }
}