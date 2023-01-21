using System.Linq.Expressions;

namespace RepositoryFramework
{
    public record LambdaFilterOperation(FilterOperations Operation, LambdaExpression? Expression) : FilteringOperation(Operation);
}
