using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;
using static Rystem.Test.UnitTest.Expression.SystemLinqExpressions;

namespace Rystem.Test.UnitTest.Expression
{
    public class SystemLinqExpressionsDeserialize
    {
        [Theory]
        [InlineData("ƒ => (((ƒ.X == \"dasda\") AndAlso ƒ.X.Contains(\"dasda\")) AndAlso ((ƒ.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (ƒ.Id == 32)))", 5)]
        [InlineData("ƒ => ((((ƒ.X == \"dasda\") AndAlso ƒ.Sol) AndAlso ƒ.X.Contains(\"dasda\")) AndAlso ((ƒ.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (ƒ.Id == 32)))", 5)]
        [InlineData("ƒ => (((((ƒ.X == \"dasda\") AndAlso ƒ.Sol) AndAlso ƒ.X.Contains(\"dasda\")) AndAlso ((ƒ.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (ƒ.Id == 32))) AndAlso ((ƒ.Type == 1) OrElse (ƒ.Type == 2)))", 5)]
        [InlineData("ƒ => (ƒ.Type == 2)", 5)]
        [InlineData("ƒ => (((((ƒ.X == \"dasda\") AndAlso ƒ.Sol) AndAlso (ƒ.X.Contains(\"dasda\") OrElse ƒ.Sol.Equals(True))) AndAlso ((ƒ.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (ƒ.Id == 32))) AndAlso ((ƒ.Type == 1) OrElse (ƒ.Type == 2)))", 5)]
        [InlineData("ƒ => ((((((ƒ.X == \"dasda\") AndAlso ƒ.Samules.Any(x => (x == \"ccccde\"))) AndAlso ƒ.Sol) AndAlso (ƒ.X.Contains(\"dasda\") OrElse ƒ.Sol.Equals(True))) AndAlso ((ƒ.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (ƒ.Id == 32))) AndAlso ((ƒ.Type == 1) OrElse (ƒ.Type == 2)))", 5)]
        [InlineData("ƒ => (ƒ.ExpirationTime > Convert.ToDateTime(\"7/6/2022 9:48:56 AM\"))", 5)]
        [InlineData("ƒ => (ƒ.TimeSpan > new TimeSpan(1000 as long))", 5)]
        [InlineData("ƒ => Not(ƒ.Inside.Inside.A.Equals(\"dasdad\"))", 0)]
        [InlineData("ƒ => Not(String.IsNullOrWhiteSpace(ƒ.Inside.Inside.A))", 5)]
        public void Test(string expressionAsString, int count)
        {
            var newExpression = expressionAsString.Deserialize<MakeIt, bool>();
            MakeIt makeIt = new()
            {
                B = "",
                Id = 32,
                E = Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2"),
                X = "dasda",
                Sol = true,
                Type = MakeType.Wrong,
                Samules = new() { "a", "b", "ccccde" },
                ExpirationTime = DateTime.Now.AddYears(24),
                TimeSpan = TimeSpan.FromTicks(100_000),
                Inside = new InsideOf() { Inside = new InsideOfInsideOf { A = "dasdad" } }
            };
            List<MakeIt> makes = new()
            {
                makeIt,
                makeIt,
                makeIt,
                makeIt,
                makeIt,
            };
            var x = makes.Where(newExpression.Compile()).ToList();
            Assert.Equal(count, x.Count);
        }
        [Theory]
        [InlineData("ƒ => ƒ.Id", 36)]
        public void OrderBy(string expressionAsString, int value)
        {
            var newExpression = expressionAsString.Deserialize<MakeIt, object>();
            MakeIt makeIt = new()
            {
                B = "",
                Id = 32,
                E = Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2"),
                X = "dasda",
                Sol = true,
                Type = MakeType.Wrong,
                Samules = new() { "a", "b", "ccccde" },
                ExpirationTime = DateTime.Now.AddYears(24),
                TimeSpan = TimeSpan.FromTicks(100_000),
                Inside = new InsideOf() { Inside = new InsideOfInsideOf { A = "dasdad" } }
            };
            List<MakeIt> makes = new()
            {
                makeIt,
                makeIt with { Id = 33},
                makeIt with { Id = 34 },
                makeIt with { Id = 35 },
                makeIt with { Id = 36 },
            };
            var x = makes.OrderByDescending(newExpression.Compile()).ToList();
            Assert.Equal(value, x.First().Id);
        }
    }
}