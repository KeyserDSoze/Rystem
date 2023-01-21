namespace System.Linq.Expressions
{
    internal interface IExpressionInterpreter
    {
        Type Type { get; }
        IEnumerable<ExpressionBearer> Read(ExpressionBearer bearer, ExpressionContext context);
    }
}