using System.Reflection;
using System.Text;

namespace RepositoryFramework.ProgrammingLanguage
{
    internal sealed class TypeScript : IProgrammingLanguage
    {
        public string End()
        {
            return "}";
        }
        public string GetMimeType()
            => "application/x-typescript";
        public string SetProperty(string name, string type)
        {
            return $"{name}: {type};";
        }
        public string GetPrimitiveType(Type type)
        {
            if (type.IsNumeric())
                return "number";
            else if (type.IsDateTime())
                return "Date";
            else if (type.IsBoolean())
                return "boolean";
            else if (type.IsEnum)
                return type.Name;
            else
                return "string";
        }
        public string GetNonPrimitiveType(Type type)
        {
            var builder = new StringBuilder();
            Write(builder, type);
            return builder.ToString();
        }
        public void Write(StringBuilder builder, Type type)
        {
            if (type.IsPrimitive())
            {
                builder.Append(GetPrimitiveType(type));
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                builder.Append("Array<");
                Write(builder, elementType!);
                builder.Append('>');
            }
            else if (type.IsEnumerable())
            {
                var elementsType = type.GetGenericArguments();
                if (type.IsDictionary())
                    builder.Append("Map<");
                else
                    builder.Append("Array<");
                for (var i = 0; i < elementsType.Length; i++)
                {
                    var element = elementsType[i];
                    Write(builder, element);
                    if (i < elementsType.Length - 1)
                        builder.Append(", ");
                }
                builder.Append('>');
            }
            else
            {
                builder.Append(type.Name);
            }
        }
        public string Start(Type type, string name)
        {
            return $"export type {name} = {{";
        }

        public string ConvertEnum(string name, Type type)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"export enum {name} {{");
            var enumUnderlyingType = Enum.GetUnderlyingType(type);
            var values = Enum.GetValues(type);
            for (var i = 0; i < values.Length; i++)
            {
                var value = values.GetValue(i)!;
                var underlyingValue = Convert.ChangeType(value, enumUnderlyingType);
                stringBuilder.AppendLine($"{value} = {underlyingValue},");
            }
            stringBuilder.AppendLine("}");
            return stringBuilder.ToString().Trim();
        }
    }
}
