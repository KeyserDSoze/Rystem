using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Test.UnitTest.Expression
{
    public class SystemLinqDynamicExpression
    {
        public class User
        {
            public int Id { get; set; }
            public decimal Price { get; set; }
        }
        private static async Task<int> GetUserIdAsync(User user)
        {
            await Task.Delay(1000);
            return user.Id;
        }
        private static async Task<decimal> GetUserPriceAsync(User user)
        {
            await Task.Delay(1000);
            return user.Price;
        }

        private static async ValueTask<int> GetUserId2Async(User user)
        {
            await Task.Delay(1000);
            return user.Id;
        }
        private static async ValueTask<decimal> GetUserPrice2Async(User user)
        {
            await Task.Delay(1000);
            return user.Price;
        }
        private static async ValueTask Wait2Async()
        {
            await Task.Delay(1000);
        }
        private static async Task WaitAsync()
        {
            await Task.Delay(1000);
        }
        [Fact]
        public async Task First()
        {
            var user = new User { Id = 1 };
            Expression<Func<User, Task<int>>> expression = x => GetUserIdAsync(x);
            LambdaExpression lambdaExpression = expression;
            var value = await lambdaExpression.InvokeAsync(typeof(int), user);
            Assert.Equal(1, value);
        }
        [Fact]
        public async Task Second()
        {
            var user = new User { Id = 1, Price = 43 };
            Expression<Func<User, Task<decimal>>> expression = x => GetUserPriceAsync(x);
            LambdaExpression lambdaExpression = expression;
            var value = await lambdaExpression.InvokeAsync<decimal>(user);
            Assert.Equal(43m, value);
        }
        [Fact]
        public async Task Third()
        {
            var user = new User { Id = 1 };
            Expression<Func<User, ValueTask<int>>> expression = x => GetUserId2Async(x);
            LambdaExpression lambdaExpression = expression;
            var value = await lambdaExpression.InvokeAsync(typeof(int), user);
            Assert.Equal(1, value);
        }
        [Fact]
        public async Task Fourth()
        {
            var user = new User { Id = 1, Price = 43 };
            Expression<Func<User, ValueTask<decimal>>> expression = x => GetUserPrice2Async(x);
            LambdaExpression lambdaExpression = expression;
            var value = await lambdaExpression.InvokeAsync<decimal>(user);
            Assert.Equal(43m, value);
        }
        [Fact]
        public async Task ThirdWithAnotherType()
        {
            var user = new User { Id = 1 };
            Expression<Func<User, ValueTask<int>>> expression = x => GetUserId2Async(x);
            LambdaExpression lambdaExpression = expression;
            var value = await lambdaExpression.InvokeAsync(typeof(decimal), user);
            Assert.Equal(1m, value);
            Assert.Equal(typeof(decimal), value!.GetType());
        }
        [Fact]
        public async Task FourthWithAnotherType()
        {
            var user = new User { Id = 1, Price = 43 };
            Expression<Func<User, ValueTask<decimal>>> expression = x => GetUserPrice2Async(x);
            LambdaExpression lambdaExpression = expression;
            var value = await lambdaExpression.InvokeAsync<int>(user);
            Assert.Equal(43, value);
            Assert.Equal(typeof(int), value.GetType());
        }
        [Fact]
        public async Task Fifth()
        {
            var user = new User { Id = 1 };
            Expression<Func<User, Task>> expression = x => WaitAsync();
            LambdaExpression lambdaExpression = expression;
            await lambdaExpression.InvokeAsync(user);
            Assert.True(true);
        }
        [Fact]
        public async Task Sixth()
        {
            var user = new User { Id = 1, Price = 43 };
            Expression<Func<User, ValueTask>> expression = x => Wait2Async();
            LambdaExpression lambdaExpression = expression;
            await lambdaExpression.InvokeAsync(user);
            Assert.True(true);
        }
        [Fact]
        public void Transformation()
        {
            Expression<Func<User, int>> expression = x => x.Id;
            var result = expression.InvokeAndTransform<User, int, decimal>(new User { Id = 13 });
            Assert.Equal(typeof(decimal), result.GetType());
            Assert.Equal(13M, result);
        }
        [Fact]
        public void Transformation2()
        {
            Expression<Func<User, int>> expression = x => x.Id;
            LambdaExpression lambda = expression;
            var result = lambda.InvokeAndTransform<decimal>(new User { Id = 13 });
            Assert.Equal(typeof(decimal), result.GetType());
            Assert.Equal(13M, result);
        }
        [Fact]
        public async Task Transformation3()
        {
            Expression<Func<User, Task<int>>> expression = x => GetUserIdAsync(x);
            LambdaExpression lambda = expression;
            var result = await lambda.InvokeAndTransformAsync<decimal>(new User { Id = 13 });
            Assert.Equal(typeof(decimal), result.GetType());
            Assert.Equal(13M, result);
        }
        [Fact]
        public void CastToAType()
        {
            Expression<Func<User, int>> expression = (User x) => x.Id;
            LambdaExpression lambdaExpression = expression;
            var asString = lambdaExpression.Serialize();
            LambdaExpression deserialized = asString.DeserializeAsDynamic<User>();
            Expression<Func<User, int>> finalExpression = deserialized.AsExpression<User, int>();
            Expression<Func<User, decimal>> finalExpression2 = deserialized.AsExpression<User, decimal>();

            List<User> users = new() { new User { Id = 13 } };
            var average = users.Average(finalExpression2.Compile());
            Assert.Equal(13, average);
        }
        [Fact]
        public void CastToAType2()
        {
            Expression<Func<User, int>> expression = (User x) => x.Id;
            LambdaExpression lambdaExpression = expression;
            var asString = lambdaExpression.Serialize();
            LambdaExpression deserialized = asString.DeserializeAsDynamic(typeof(User));
            Expression<Func<User, int>> finalExpression = deserialized.AsExpression<User, int>();

            Expression<Func<User, decimal>> finalExpression2 = deserialized.AsExpression<User, decimal>();

            List<User> users = new() { new User { Id = 13 } };
            var average = users.Average(finalExpression2.Compile());
            Assert.Equal(13, average);
        }
    }
}