using System.Linq.Expressions;
using Xunit;

namespace Rystem.Test.UnitTest.Expression
{
    public class SystemLinqDynamicDeserialize
    {
        public class User
        {
            public int Id { get; set; }
        }
        [Theory]
        [InlineData("ƒ => ƒ.Id", 36)]
        public void First(string expressionAsString, int value)
        {
            var output = expressionAsString.DeserializeAsDynamicAndRetrieveType<User>();
            var newExpression = output.Expression;
            var result = (int)newExpression.Compile().DynamicInvoke(new User { Id = value })!;
            Assert.Equal(value, result);
            Assert.Equal(value.GetType(), output.Type);
        }
        [Theory]
        [InlineData("ƒ => ƒ.Id == 25", true)]
        [InlineData("ƒ => (ƒ.Id == 25 AndAlso ƒ.Id >= 25)", true)]
        public void Second(string expressionAsString, bool value)
        {
            var output = expressionAsString.DeserializeAsDynamicAndRetrieveType<User>();
            var newExpression = output.Expression;
            var result = newExpression.Compile().DynamicInvoke(new User { Id = 25 });
            Assert.Equal(value, result);
            Assert.Equal(value.GetType(), output.Type);
        }
    }
}