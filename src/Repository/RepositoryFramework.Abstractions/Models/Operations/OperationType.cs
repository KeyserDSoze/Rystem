using System.Linq.Expressions;

namespace RepositoryFramework
{
    public sealed class OperationType<TProperty>
    {
        public static OperationType<TProperty> Count { get; } = new(DefaultOperations.Count);
        public static OperationType<TProperty> Sum { get; } = new(DefaultOperations.Sum);
        public static OperationType<TProperty> Max { get; } = new(DefaultOperations.Max);
        public static OperationType<TProperty> Min { get; } = new(DefaultOperations.Min);
        public static OperationType<TProperty> Average { get; } = new(DefaultOperations.Average);
        public string Name { get; }
        public OperationType(string operationName)
        {
            Name = operationName;
        }
        private static readonly Type s_type = typeof(TProperty);
        public Type Type => s_type;
        public ValueTask<TProperty?> ExecuteDefaultOperationAsync(
            Delegate count,
            Delegate sum,
            Delegate max,
            Delegate min,
            Delegate average)
            => Name switch
            {
                DefaultOperations.Count => count.InvokeAsync<TProperty>(),
                DefaultOperations.Sum => sum.InvokeAsync<TProperty>(),
                DefaultOperations.Max => max.InvokeAsync<TProperty>(),
                DefaultOperations.Min => min.InvokeAsync<TProperty>(),
                DefaultOperations.Average => average.InvokeAsync<TProperty>(),
                _ => throw new NotImplementedException($"{Name} is not a default operation.")
            };
    }
}
