using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.UnitTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Test.UnitTest.Linq
{
    public class SystemLinq
    {
        public class ForUserSecrets { }
        private static readonly IServiceProvider _serviceProvider;
        static SystemLinq()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
               .AddUserSecrets<ForUserSecrets>()
               .Build();
            services.AddSingleton(configuration);
            services.AddDbContext<SampleContext>(options =>
            {
                options.UseSqlServer(configuration["Database:ConnectionString"]);
            }, ServiceLifetime.Scoped);
            _serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
        }
        internal enum MakeType
        {
            No,
            Yes,
            Wrong
        }
        internal sealed record MakeIt
        {
            public int Id { get; set; }
            public double Value { get; set; }
            public string? B { get; set; }
            public Guid E { get; set; }
            public bool Sol { get; set; }
            public MakeType Type { get; set; }
            public List<string>? Samules { get; set; }
            public DateTime ExpirationTime { get; set; }
            public TimeSpan TimeSpan { get; set; }
        }
        private readonly SampleContext _context;
        public SystemLinq()
        {
            _context = _serviceProvider.GetService<SampleContext>()!;
        }

        [Fact]
        public void Test1()
        {
            List<MakeIt> makes = new();
            for (int i = 0; i < 100; i++)
                makes.Add(new MakeIt { Id = i, Value = i });

            Expression<Func<MakeIt, int>> expression = x => x.Id;
            string value = expression.Serialize();
            LambdaExpression newLambda = value.DeserializeAsDynamic<MakeIt>();
            var got = makes.AsQueryable();
            var cut = got.OrderByDescending(newLambda).ThenByDescending(newLambda).ToList();
            Assert.Equal(99, cut.First().Id);
            Assert.Equal(98, cut.Skip(1).First().Id);
            cut = cut.AsQueryable().OrderBy(newLambda).ThenBy(newLambda).ToList();
            Assert.Equal(0, cut.First().Id);
            Assert.Equal(1, cut.Skip(1).First().Id);
            var queryable = cut.AsQueryable();
            var average1 = (decimal)Convert.ChangeType(queryable.Average(x => x.Id), typeof(decimal));
            var average = queryable.Average(newLambda);
            Assert.Equal(average1, average);
            Expression<Func<MakeIt, bool>> expression2 = x => x.Id >= 10;
            string value2 = expression2.Serialize();
            LambdaExpression newLambda2 = value2.DeserializeAsDynamic<MakeIt>();
            var count = queryable.Count(newLambda2);
            var count2 = queryable.LongCount(newLambda2);
            Assert.Equal(count, count2);
            Assert.Equal(90, count);
            var max = queryable.Max(newLambda);
            Assert.Equal(99, max);
            var min = queryable.Min(newLambda);
            Assert.Equal(0, min);
            var sum1 = queryable.Sum(x => x.Id);
            var sum2 = queryable.Sum(newLambda);
            Assert.Equal(sum1, sum2);
            var where = queryable.Where(newLambda2);
            Assert.Equal(90, where.Count());
            Expression<Func<MakeIt, int>> expression3 = x => x.Id / 10;
            string value3 = expression3.Serialize();
            LambdaExpression newLambda3 = value3.DeserializeAsDynamic<MakeIt>();
            var distincted1 = queryable.DistinctBy(x => x.Id / 10);
            var distincted2 = queryable.DistinctBy(newLambda3);
            Assert.Equal(distincted1.Count(), distincted2.Count());
            Assert.Equal(10, distincted2.Count());
            var selected = queryable.Select(newLambda);
            Assert.Equal(0, selected.First());
            Assert.Equal(typeof(int), selected.First().GetType());
            var selected2 = queryable.Select<MakeIt, decimal>(newLambda);
            Assert.Equal(0M, selected2.First());
            var grouped1 = queryable.GroupBy(x => x.Id / 10);
            var grouped2 = queryable.GroupBy(newLambda3);
            Assert.Equal(grouped1.Count(), grouped2.Count());
            Assert.Equal(10, grouped2.Count());
            Assert.Equal(0, grouped2.First().Key);
            Assert.Equal(2, grouped2.Skip(2).First().Key);
            var grouped3 = queryable.GroupBy<decimal, MakeIt>(newLambda3);
            Assert.Equal(grouped1.Count(), grouped3.Count());
            Assert.Equal(10, grouped3.Count());
            Assert.Equal(0M, grouped3.First().Key);
            Assert.Equal(2M, grouped3.Skip(2).First().Key);

            Expression<Func<MakeIt, double>> expressionD = x => x.Value;
            string valueD = expressionD.Serialize();
            LambdaExpression newLambdaD = valueD.DeserializeAsDynamic<MakeIt>();
            var gotD = makes.AsQueryable();
            var cutD = gotD.OrderByDescending(newLambdaD).ThenByDescending(newLambdaD).ToList();
            Assert.Equal(99D, cutD.First().Value);
            Assert.Equal(98D, cutD.Skip(1).First().Value);
            cutD = cutD.AsQueryable().OrderBy(newLambdaD).ThenBy(newLambdaD).ToList();
            Assert.Equal(0D, cutD.First().Value);
            Assert.Equal(1D, cutD.Skip(1).First().Value);
            var queryableD = cutD.AsQueryable();
            var averageD1 = (decimal)Convert.ChangeType(queryableD.Average(x => x.Value), typeof(decimal));
            var averageD = queryableD.Average(newLambdaD);
            Assert.Equal(averageD1, averageD);
            Expression<Func<MakeIt, bool>> expressionD2 = x => x.Value >= 10;
            string valueD2 = expressionD2.Serialize();
            LambdaExpression newLambdaD2 = valueD2.DeserializeAsDynamic<MakeIt>();
            var countD = queryableD.Count(newLambdaD2);
            var countD2 = queryableD.LongCount(newLambdaD2);
            Assert.Equal(countD, countD2);
            Assert.Equal(90, countD);
            var maxD = queryableD.Max(newLambdaD);
            Assert.Equal(99D, maxD);
            var minD = queryableD.Min(newLambdaD);
            Assert.Equal(0D, minD);
            var sumD1 = queryableD.Sum(x => x.Id);
            var sumD2 = queryableD.Sum(newLambdaD);
            Assert.Equal(sumD1, sumD2);
            var whereD = queryableD.Where(newLambdaD2);
            Assert.Equal(90, whereD.Count());
            Expression<Func<MakeIt, double>> expressionD3 = x => x.Value / 10;
            string valueD3 = expressionD3.Serialize();
            LambdaExpression newLambdaD3 = valueD3.DeserializeAsDynamic<MakeIt>();
            var distinctedD1 = queryableD.DistinctBy(x => x.Value / 10);
            var distinctedD2 = queryableD.DistinctBy(newLambdaD3);
            Assert.Equal(distinctedD1.Count(), distinctedD2.Count());
            Assert.Equal(100, distinctedD2.Count());
            var selectedD = queryableD.Select(newLambdaD);
            Assert.Equal(0D, selectedD.First());
            var selectedD2 = queryableD.Select<MakeIt, decimal>(newLambdaD);
            Assert.Equal(0M, selectedD2.First());
            var groupedD1 = queryableD.GroupBy(x => x.Value / 10);
            var groupedD2 = queryableD.GroupBy(newLambdaD3);
            Assert.Equal(groupedD1.Count(), groupedD2.Count());
            Assert.Equal(100, groupedD2.Count());
            Assert.Equal(0D, groupedD2.First().Key);
            Assert.Equal(0.2D, groupedD2.Skip(2).First().Key);
            var groupedD3 = queryableD.GroupBy<decimal, MakeIt>(newLambdaD3);
            Assert.Equal(groupedD1.Count(), groupedD3.Count());
            Assert.Equal(100, groupedD3.Count());
            Assert.Equal(0M, groupedD3.First().Key);
            Assert.Equal(0.2M, groupedD3.Skip(2).First().Key);
        }

        [Fact]
        public async Task Test2()
        {
            List<MakeIt> makes = new();
            for (int i = 0; i < 100; i++)
                makes.Add(new MakeIt { Id = i, Value = i });
            IQueryable<MakeIt> items = makes.AsQueryable();

            Expression<Func<MakeIt, bool>> expression = x => x.Value >= 10;
            string value = expression.Serialize();
            LambdaExpression newLambda = value.DeserializeAsDynamic<MakeIt>();

            var result = await items.CallMethodAsync<MakeIt, IQueryable<MakeIt>>("GetAsync", typeof(QueryableExtensions));
            var result2 = await items.CallMethodAsync<MakeIt, IQueryable<MakeIt>>("GetAsync", newLambda, typeof(QueryableExtensions));

            Assert.Equal(100, result.Count());
            Assert.Equal(90, result2.Count());
        }
        [Fact]
        public async Task Test3()
        {
            var users = await _context.Users.ToListAsync().NoContext();
            foreach (var user in users)
                _context.Users.Remove(user);
            await _context.SaveChangesAsync().NoContext();
            for (int i = 1; i <= 10; i++)
                _context.Users.Add(new User
                {
                    Identificativo = i,
                    Cognome = i.ToString(),
                    Nome = i.ToString(),
                    IndirizzoElettronico = i.ToString(),
                });
            await _context.SaveChangesAsync().NoContext();

            Expression<Func<User, int>> expression = x => x.Identificativo;
            string value = expression.Serialize();
            LambdaExpression newLambda = value.DeserializeAsDynamic<User>();

            //var max3 = _context.Users.Select(newLambda).MaxAsync().NoContext();
            var selected = _context.Users.Select(newLambda);
            var max = await _context.Users.Select(newLambda).CallMethodAsync("MaxAsync", typeof(EntityFrameworkQueryableExtensions)).NoContext();
            var max2 = await selected.MaxAsync().NoContext();
            Assert.Equal(max, max2);
        }
        [Fact]
        public async Task CheckAsyncIntegrationAsync()
        {
            List<MakeIt> makes = new();
            for (int i = 0; i < 100; i++)
                makes.Add(new MakeIt { Id = i, Value = i });

            var checkAllIdMoreThanZeroOrZero = await makes.AllAsync(x => ValueTask.FromResult(x.Id >= 0));
            Assert.True(checkAllIdMoreThanZeroOrZero);
            checkAllIdMoreThanZeroOrZero = await makes.AllAsync(x => Task.FromResult(x.Id >= 0));
            Assert.True(checkAllIdMoreThanZeroOrZero);

            var checkAnyIdIsZero = await makes.AnyAsync(x => ValueTask.FromResult(x.Id == 0));
            Assert.True(checkAnyIdIsZero);
            checkAnyIdIsZero = await makes.AnyAsync(x => Task.FromResult(x.Id == 0));
            Assert.True(checkAnyIdIsZero);

            var checkIfAllIdMoreThanZero = await makes.AllAsync(x => ValueTask.FromResult(x.Id > 0));
            Assert.False(checkIfAllIdMoreThanZero);
            checkIfAllIdMoreThanZero = await makes.AllAsync(x => Task.FromResult(x.Id > 0));
            Assert.False(checkIfAllIdMoreThanZero);

            var checkAnyIdIsMinusOne = await makes.AnyAsync(x => ValueTask.FromResult(x.Id == -1));
            Assert.False(checkAnyIdIsMinusOne);
            checkAnyIdIsMinusOne = await makes.AnyAsync(x => Task.FromResult(x.Id == -1));
            Assert.False(checkAnyIdIsMinusOne);
        }
    }
}