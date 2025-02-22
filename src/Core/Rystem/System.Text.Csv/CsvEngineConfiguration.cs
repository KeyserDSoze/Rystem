using System.Linq.Expressions;

namespace System.Text.Csv
{
    public sealed class CsvEngineConfiguration<T>
    {
        public bool UseHeader { get; set; } = true;
        public string Delimiter { get; set; } = ",";
        public bool ForExcel { get; set; } = false;
        public bool UseExtendedName { get; set; } = true;
        internal Dictionary<string, string> Headers { get; } = [];
        internal Dictionary<string, bool> ToAvoid { get; } = [];
        private const string FirstValue = "First().";
        public CsvEngineConfiguration<T> ConfigureHeader<TProperty>(Expression<Func<T, TProperty>> propertyExpression, string name)
        {
            if (propertyExpression.Body is MemberExpression memberExpression)
            {
                var propertyName = string.Join('.', memberExpression.ToString().Replace(FirstValue, string.Empty).Split('.').Skip(1));
                if (!Headers.TryAdd(propertyName, name))
                    Headers[propertyName] = name;
            }
            else
            {
                throw new ArgumentException("Invalid property expression. Please provide a direct property access.");
            }
            return this;
        }
        public CsvEngineConfiguration<T> AvoidProperty<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpression)
            {
                var propertyName = string.Join('.', memberExpression.ToString().Replace(FirstValue, string.Empty).Split('.').Skip(1));
                if (!ToAvoid.TryAdd(propertyName, true))
                    ToAvoid[propertyName] = true;
            }
            else
            {
                throw new ArgumentException("Invalid property expression. Please provide a direct property access.");
            }
            return this;
        }
    }
}
