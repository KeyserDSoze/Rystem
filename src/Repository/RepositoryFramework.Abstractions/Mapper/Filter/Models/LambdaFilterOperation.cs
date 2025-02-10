using System.Linq.Expressions;

namespace RepositoryFramework
{
    public record LambdaFilterOperation(FilterOperations Operation, FilterRequest Request, LambdaExpression? Expression) : FilteringOperation(Operation, Request);
}
