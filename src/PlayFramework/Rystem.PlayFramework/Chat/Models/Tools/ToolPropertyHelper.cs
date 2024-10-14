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
                    Description = description?.Description ?? parameterName ?? type.Name,
                    Type = type.IsNumeric() ? "number" : "string"
                });
            }
            else
            {
                var innerFunction = new ToolNonPrimitiveProperty()
                {
                    Description = description?.Description ?? parameterName ?? type.Name,
                };
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
