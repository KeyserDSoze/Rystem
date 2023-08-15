using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    internal static class ProgrammingLanguageExtensions
    {
        public static ProgrammingLanguangeResponse ConvertAs(this IEnumerable<Type> types, ProgrammingLanguage programmingLanguage)
        {
            IProgrammingLanguage programming = new TypeScript();
            switch (programmingLanguage)
            {
                default:
                    break;
            }
            var stringBuilder = new StringBuilder();
            var typesAlreadyAdded = new Dictionary<Type, bool>();
            var namesAlreadyAdded = new Dictionary<string, int>();
            foreach (var type in types)
            {
                stringBuilder.AppendLine(Transform(type.Name, type, programming, typesAlreadyAdded, namesAlreadyAdded));
            }
            return new()
            {
                Text = stringBuilder.ToString(),
                MimeType = programming.GetMimeType()
            };
        }
        public static ProgrammingLanguangeResponse ConvertAs(this Type type, ProgrammingLanguage programmingLanguage, string? name = null)
        {
            IProgrammingLanguage programming = new TypeScript();
            switch (programmingLanguage)
            {
                default:
                    break;
            }
            return new()
            {
                Text = Transform(name ?? type.Name, type, programming, new(), new()),
                MimeType = programming.GetMimeType()
            };
        }
        private static string Transform(string name, Type type, IProgrammingLanguage programmingLanguage, Dictionary<Type, bool> typesAlreadyAdded, Dictionary<string, int> namesAlreadyAdded)
        {
            if (!typesAlreadyAdded.ContainsKey(type))
            {
                typesAlreadyAdded.Add(type, true);
                if (namesAlreadyAdded.ContainsKey(name))
                {
                    name = $"{name}{namesAlreadyAdded[name]}";
                    namesAlreadyAdded[name]++;
                }
                else
                    namesAlreadyAdded.Add(name, 2);
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(programmingLanguage.Start(name));
                var properties = type.GetProperties();
                var complexObject = new List<string>();
                foreach (var property in properties)
                {
                    var propertyName = GetName(property);
                    if (property.PropertyType.IsPrimitive())
                    {
                        stringBuilder.AppendLine(
                            programmingLanguage
                                .SetProperty(propertyName,
                                    programmingLanguage.GetPrimitiveType(property.PropertyType)));
                    }
                    else
                    {
                        stringBuilder.AppendLine(
                            programmingLanguage
                                .SetProperty(propertyName,
                                    programmingLanguage.GetNonPrimitiveType(property.PropertyType)));
                        foreach (var interpretation in GetFurtherTypes(property.PropertyType))
                            complexObject.Add(Transform(interpretation.Name, interpretation, programmingLanguage, typesAlreadyAdded, namesAlreadyAdded));
                    }
                }
                stringBuilder.AppendLine(programmingLanguage.End());
                foreach (var finalization in complexObject)
                {
                    stringBuilder.AppendLine(finalization);
                }
                return stringBuilder.ToString();
            }
            return string.Empty;
        }
        private static IEnumerable<Type> GetFurtherTypes(Type startingType)
        {
            if (startingType.IsArray)
            {
                var elementType = startingType.GetElementType();
                foreach (var type in GetFurtherTypes(elementType!))
                    yield return type;
            }
            else if (startingType.IsEnumerable())
            {
                var elementsType = startingType.GetGenericArguments();
                foreach (var element in elementsType)
                {
                    foreach (var type in GetFurtherTypes(element))
                        yield return type;
                }
            }
            else
            {
                yield return startingType;
            }
        }
        private static string GetName(PropertyInfo property)
        {
            var propertyName = property.Name;
            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonName != null)
                propertyName = jsonName.Name;
            return propertyName;
        }
        private static string GetClassName(PropertyInfo property)
        {
            var propertyName = property.Name;
            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonName != null)
                propertyName = jsonName.Name;
            return propertyName;
        }
    }
}
