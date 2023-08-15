using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace RepositoryFramework.Api.Server.TypescriptModelsCreatorEngine
{
    public sealed class TypescriptModelCreatorEngine
    {
        private sealed class ModelInterpretation
        {
            public Type Type { get; set; }
            public string Name { get; set; }
        }
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
                    var interpretations = new List<ModelInterpretation> { new() { Type = property.PropertyType, Name = propertyName } };
                    var objectName = "{0}";
                    if (property.PropertyType.IsArray)
                    {
                        interpretations = new List<Type> { property.PropertyType };
                        objectName = "array<{0}>";
                    }
                    else if (property.PropertyType.IsDictionary())
                        objectName = "map<{0},{1}>";
                    else if (property.PropertyType.IsEnumerable())
                        objectName = "array<{0}>";
                    stringBuilder.AppendLine($"{propertyName}: {string.Format(objectName, propertyName)};");
                    foreach (var interpretation in interpretations)
                        complexObject.Add(Transform(interpretation.Name, interpretation.Type));
                }
            }
            stringBuilder.AppendLine("}");
            foreach (var finalization in complexObject)
            {
                stringBuilder.AppendLine(finalization);
            }
            return stringBuilder.ToString();
        }
        private string GetName(PropertyInfo property)
        {
            var propertyName = property.Name;
            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonName != null)
                propertyName = jsonName.Name;
            return propertyName;
        }
        private string GetClassName(PropertyInfo property)
        {
            var propertyName = property.Name;
            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonName != null)
                propertyName = jsonName.Name;
            return propertyName;
        }
    }
}
