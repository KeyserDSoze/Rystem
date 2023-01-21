using System.Reflection;

namespace System.Linq.Expressions
{
    internal sealed record ExpressionBearer(Expression Expression)
    {
        public MemberInfo? Member { get; set; }
        public string? Key { get; set; }
    }
}