using System.Reflection;
using System.Text;

namespace RepositoryFramework
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
            else if(type.IsBoolean())
                return "boolean";
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
        public string Start(string name)
        {
            return $"export type {name} = {{";
        }
    }
}
