using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Rystem.Test.UnitTest.Expression
{
    public class SystemLinqExpressions
    {
        internal enum MakeType
        {
            No,
            Yes,
            Wrong
        }
        internal sealed record MakeIt
        {
            public string? X { get; set; }
            public int Id { get; set; }
            public string? B { get; set; }
            public Guid E { get; set; }
            public bool Sol { get; set; }
            public MakeType Type { get; set; }
            public List<string>? Samules { get; set; }
            public DateTime ExpirationTime { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public InsideOf? Inside { get; set; }
        }
        public class InsideOf
        {
            public string? A { get; set; }
            public InsideOfInsideOf? Inside { get; set; }
        }
        public class InsideOfInsideOf
        {
            public string? A { get; set; }
        }
        public class IperUser : User
        {
            public IperUser(string email) : base(email)
            {
            }
        }
        public class SuperUser : User
        {
            public SuperUser(string email) : base(email)
            {
            }
        }
        public class User
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Email { get; }
            public int Port { get; set; }
            public bool IsAdmin { get; set; }
            public Guid GroupId { get; set; }
            public DateTime ExpirationTime { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public User(string email)
            {
                Email = email;
            }
        }
        private const int V = 32;
        [Fact]
        public void Test1()
        {
            string result = "� => (((�.X == \"dasda\") AndAlso �.X.Contains(\"dasda\")) AndAlso ((�.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (�.Id == 32)))";
            var q = "dasda";
            var id = Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2");
            Expression<Func<MakeIt, bool>> expression = � => �.X == q && �.X.Contains(q) && (�.E == id | �.Id == V);
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
        [Fact]
        public void Test2()
        {
            string result = "� => ((((�.X == \"dasda\") AndAlso �.Sol) AndAlso �.X.Contains(\"dasda\")) AndAlso ((�.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (�.Id == 32)))";
            var q = "dasda";
            var id = Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2");
            Expression<Func<MakeIt, bool>> expression = � => �.X == q && �.Sol && �.X.Contains(q) && (�.E == id | �.Id == V);
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
        [Fact]
        public void Test3()
        {
            string result = "� => (((((�.X == \"dasda\") AndAlso �.Sol) AndAlso �.X.Contains(\"dasda\")) AndAlso ((�.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (�.Id == 32))) AndAlso ((�.Type == 1) OrElse (�.Type == 2)))";
            var q = "dasda";
            var id = Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2");
            var qq = MakeType.Wrong;
            Expression<Func<MakeIt, bool>> expression = � => �.X == q && �.Sol && �.X.Contains(q) && (�.E == id | �.Id == V) && (�.Type == MakeType.Yes || �.Type == qq);
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
        private const bool IsOk = true;
        [Fact]
        public void Test4()
        {
            string result = "� => (((((�.X == \"dasda\") AndAlso �.Sol) AndAlso (�.X.Contains(\"dasda\") OrElse �.Sol.Equals(True))) AndAlso ((�.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (�.Id == 32))) AndAlso ((�.Type == 1) OrElse (�.Type == 2)))";
            var q = "dasda";
            var id = Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2");
            var qq = MakeType.Wrong;
            Expression<Func<MakeIt, bool>> expression = � => �.X == q && �.Sol && (�.X.Contains(q) || �.Sol.Equals(IsOk)) && (�.E == id | �.Id == V) && (�.Type == MakeType.Yes || �.Type == qq);
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
        [Fact]
        public void Test5()
        {
            string result = "� => ((((((�.X == \"dasda\") AndAlso �.Samules.Any(x => (x == \"ccccde\"))) AndAlso �.Sol) AndAlso (�.X.Contains(\"dasda\") OrElse �.Sol.Equals(True))) AndAlso ((�.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (�.Id == 32))) AndAlso ((�.Type == 1) OrElse (�.Type == 2)))";
            var q = "dasda";
            var k = "ccccde";
            var id = Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2");
            var qq = MakeType.Wrong;
            Expression<Func<MakeIt, bool>> expression = � => �.X == q && �!.Samules!.Any(x => x == k) && �.Sol && (�.X.Contains(q) || �.Sol.Equals(IsOk)) && (�.E == id | �.Id == V) && (�.Type == MakeType.Yes || �.Type == qq);
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
        [Fact]
        public void Test6()
        {
            string result = "x => x.Email.Contains(\"@gmail.com\")";
            var user = new User("@gmail.com");
            Expression<Func<User, bool>> expression = x => x.Email!.Contains(user.Email!);
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
        [Fact]
        public void Test7()
        {
            string result = "x => (x.ExpirationTime > Convert.ToDateTime(\"10/12/2021 11:13:09 AM\"))";
            DateTime start = new(2021, 10, 12, 11, 13, 9);
            Expression<Func<User, bool>> expression = x => x.ExpirationTime > start;
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
        [Fact]
        public void Test8()
        {
            string result = "x => (x.TimeSpan > new TimeSpan(1000 as long))";
            TimeSpan start = TimeSpan.FromTicks(1000);
            Expression<Func<User, bool>> expression = x => x.TimeSpan > start;
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
        [Fact]
        public void Test9()
        {
            string result = "� => Not(�.Inside.Inside.A.Equals(\"\"))";
            string olaf = "";
            Expression<Func<MakeIt, bool>> expression = � => !�!.Inside!.Inside!.A!.Equals(olaf);
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
        [Fact]
        public void Test10()
        {
            string result = "� => Not(String.IsNullOrWhiteSpace(�.Inside.Inside.A))";
            Expression<Func<MakeIt, bool>> expression = � => !string.IsNullOrWhiteSpace(�!.Inside!.Inside!.A);
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
        [Fact]
        public void Test11()
        {
            string result = "Param_0 => Param_0.Id";
            Expression<Func<MakeIt, object>> expression = "� => �.Id".Deserialize<MakeIt, object>();
            var serialized = expression.Serialize();
            Assert.Equal(result, serialized);
        }
    }
}