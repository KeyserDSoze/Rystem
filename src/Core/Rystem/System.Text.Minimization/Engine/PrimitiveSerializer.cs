using System.Globalization;
using System.Reflection;

namespace System.Text.Minimization
{
    internal class PrimitiveSerializer : IMinimizationInterpreter
    {
        public int Priority => 6;
        public bool IsValid(Type type) => type.IsNumeric() || type == typeof(Guid) || type == typeof(Guid?)
                || type == typeof(char) || type == typeof(char?) || type == typeof(byte) || type == typeof(byte?) || type == typeof(sbyte) || type == typeof(sbyte?)
                || type == typeof(bool) || type == typeof(bool?) || type == typeof(string) || type == typeof(DateTime) || type == typeof(DateTime?) || type == typeof(TimeSpan)
                || type == typeof(TimeSpan?) || type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?) || type.IsEnum;
        public dynamic Deserialize(Type type, string value, int deep = int.MaxValue)
        {
            if (type == typeof(string))
                return value;
            else if (type == typeof(Guid))
                return Guid.Parse(value);

            if (string.IsNullOrWhiteSpace(value))
                if (type.IsValueType)
                    return Activator.CreateInstance(type)!;
                else
                    return null!;

            if (!type.IsEnum)
            {
                return (!string.IsNullOrWhiteSpace(value) ?
                    (!type.IsGenericType ?
                        Convert.ChangeType(value, type, CultureInfo.InvariantCulture) :
                        Convert.ChangeType(value, type.GenericTypeArguments[0], CultureInfo.InvariantCulture)
                    )
                    : default)!;
            }
            else
                return Enum.Parse(type, value);
        }

        public string Serialize(Type type, object value, int deep)
            => value?.ToString() ?? string.Empty;
    }
}
