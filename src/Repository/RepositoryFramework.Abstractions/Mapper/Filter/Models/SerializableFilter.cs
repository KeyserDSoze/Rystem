using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    public sealed class SerializableFilter
    {
        [JsonPropertyName("o")]
        public List<FilterOperationAsString> Operations { get; init; } = new();
        public static SerializableFilter Empty => new();
        public IFilterExpression Deserialize<T>()
        {
            FilterExpression query = new();
            if (Operations != null)
                foreach (var operation in Operations)
                {
                    if (operation.Operation == FilterOperations.Top || operation.Operation == FilterOperations.Skip)
                        query.Operations.Add(new ValueFilterOperation(operation.Operation, operation.Value != null ? long.Parse(operation.Value) : null));
                    else
                        query.Operations.Add(new LambdaFilterOperation(operation.Operation, operation.Value?.DeserializeAsDynamic<T>()));
                }
            return query;
        }
        public IFilterExpression DeserializeAndTranslate(IRepositoryFilterTranslator translator)
            => translator.Transform(this);
        public string AsString()
            => string.Join('_', Operations.Select(x => $"{x.Operation}{x.Value}"));
    }
}
