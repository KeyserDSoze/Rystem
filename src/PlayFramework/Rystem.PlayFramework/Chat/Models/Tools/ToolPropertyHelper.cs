using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Rystem.PlayFramework
{
    public static class ToolPropertyHelper
    {
        public static void Add(string? parameterName, Type type, ToolNonPrimitiveProperty jsonFunction)
        {
            var description = type.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
            if (type.IsPrimitive())
            {
                jsonFunction.AddPrimitive(parameterName ?? type.Name, new ToolProperty
                {
                    Type = type.IsNumeric() ? "number" : "string"
                });
            }
            else
            {
                var innerFunction = new ToolNonPrimitiveProperty();
                jsonFunction.AddObject(parameterName ?? type.Name, innerFunction);
                foreach (var innerParameter in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (innerParameter.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                    {
                        Add(innerParameter.Name, innerParameter.PropertyType, innerFunction);
                    }
                }
            }
        }
    }
}
