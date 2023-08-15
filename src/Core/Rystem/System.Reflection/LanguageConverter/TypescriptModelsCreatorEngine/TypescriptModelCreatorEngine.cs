using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace RepositoryFramework.Api.Server.TypescriptModelsCreatorEngine
{
    public sealed class TypescriptModelCreatorEngine
    {
        public string Transform(string? name, Type type)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"export type {name ?? type.Name} = {{");
            var properties = type.GetProperties();
            var complexObject = new List<string>();
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (jsonName != null)
                    propertyName = jsonName.Name;
                if (property.PropertyType.IsPrimitive())
                {
                    stringBuilder.AppendLine($"{propertyName}: {(property.PropertyType.IsNumeric() ? "number" : "string")};");
                }
                else
                {
                    var currentTypes = new List<Type> { property.PropertyType };
                    var objectName = "{0}";
                    if (property.PropertyType.IsArray)
                        objectName = "array<{0}>";
                    else if (property.PropertyType.IsDictionary())
                        objectName = "array<{0}>";
                    else if (property.PropertyType.IsEnumerable())
                        objectName = "map<{0},{1}>";
                    stringBuilder.AppendLine($"{propertyName}: {string.Format(objectName, propertyName)};");
                    foreach (var currentType in currentTypes)
                        complexObject.Add(Transform(propertyName, currentType));
                }
            }
            stringBuilder.AppendLine("}");
            foreach (var finalization in complexObject)
            {
                stringBuilder.AppendLine(finalization);
            }
            return stringBuilder.ToString();
        }
    }
}
