using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Rystem.Api;

public static class OpenApiSchemaGenerator
{
    /// <summary>
    /// Generates an OpenAPI schema for a given object.
    /// </summary>
    /// <typeparam name="T">The type of the object to generate the schema for.</typeparam>
    /// <returns>An OpenApiSchema representing the object's structure.</returns>
    public static OpenApiSchema GenerateOpenApiSchema<T>(this T entity)
        => GenerateOpenApiSchema(typeof(T), entity);
    /// <summary>
    /// Generates an OpenAPI schema for a given object.
    /// </summary>
    /// <typeparam name="T">The type of the object to generate the schema for.</typeparam>
    /// <returns>An OpenApiSchema representing the object's structure.</returns>
    public static OpenApiSchema GenerateOpenApiSchema(this object entity)
        => GenerateOpenApiSchema(entity.GetType(), entity);

    /// <summary>
    /// Generates an OpenAPI schema for a given type.
    /// </summary>
    /// <param name="type">The type to generate the schema for.</param>
    /// <returns>An OpenApiSchema representing the type's structure.</returns>
    public static OpenApiSchema GenerateOpenApiSchema(this Type type, object? example = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        // Check for primitive types
        if (type.IsEnum)
        {
            return new OpenApiSchema
            {
                Type = "string",
                Enum = [.. Enum.GetNames(type).Select(name => new OpenApiString(name, false))],
                Example = GetExample(true, example)
            };
        }
        else if (type.IsPrimitive())
        {
            return new OpenApiSchema
            {
                Type = MapTypeToOpenApiType(type),
                Format = MapTypeToOpenApiFormat(type),
                Example = GetExample(true, example)
            };
        }
        else if (type.IsTheSameTypeOrASon(typeof(IHttpFile)) || type.IsTheSameTypeOrASon(typeof(IFormFile)))
        {
            return new OpenApiSchema
            {
                Type = "string",
                Format = "binary",
            };
        }
        else if (type.IsDictionary())
        {
            var valueType = type.GetGenericArguments()[1];
            return new OpenApiSchema
            {
                Type = "object",
                AdditionalProperties = valueType != null ? valueType.GenerateOpenApiSchema(null) : null,
                Example = GetExample(false, example)
            };
        }
        else if (type.IsTheSameTypeOrASon(typeof(byte[])))
        {
            return new OpenApiSchema
            {
                Type = "string",
                Format = "byte",
            };
        }
        else if (type.IsArray || type.IsEnumerable())
        {
            var itemType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            return new OpenApiSchema
            {
                Type = "array",
                Items = itemType != null ? itemType.GenerateOpenApiSchema(null) : null,
                Example = GetExample(false, example)
            };
        }
        else if (type.IsTheSameTypeOrASon(typeof(Stream)))
        {
            return new OpenApiSchema
            {
                Type = "string",
                Format = "binary",
            };
        }
        else
        {
            // Handle complex objects
            var schema = new OpenApiSchema
            {
                Type = "object",
                Required = new HashSet<string>(),
                Example = GetExample(false, example)
            };
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                // Generate schema for each property
                var propertySchema = property.PropertyType.GenerateOpenApiSchema(null);
                schema.Properties.Add(property.Name, propertySchema);

                // Add to required if property is not nullable
                if (!IsNullableProperty(property))
                {
                    schema.Required.Add(property.Name);
                }
            }
            return schema;
        }
    }
    public static string GetContentType(this Type type)
    {
        if (type.IsTheSameTypeOrASon(typeof(IHttpFile)) || type.IsTheSameTypeOrASon(typeof(IFormFile)) || type.IsTheSameTypeOrASon(typeof(byte[])) || type.IsTheSameTypeOrASon(typeof(Stream)))
            return "application/octet-stream";
        else if (type.IsPrimitive())
            return "application/text";
        else
            return "application/json";
    }
    private static OpenApiString? GetExample(bool isPrimitive, object? entity)
    {
        if (entity != null)
            return new OpenApiString(isPrimitive ? entity.ToString() : entity.ToJson(DefaultJsonSettings.ForEnum), false);
        return null;
    }

    /// <summary>
    /// Maps a C# type to an OpenAPI type.
    /// </summary>
    private static string MapTypeToOpenApiType(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(bool) || type == typeof(bool?))
            return "boolean";
        if (type == typeof(int) || type == typeof(long) || type == typeof(int?) || type == typeof(long?))
            return "integer";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal) || type == typeof(float?) || type == typeof(double?) || type == typeof(decimal?))
            return "number";
        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return "string";
        return "object";
    }

    /// <summary>
    /// Maps a C# type to an OpenAPI format (if applicable).
    /// </summary>
    private static string MapTypeToOpenApiFormat(Type type)
    {
        if (type == typeof(int) || type == typeof(int?))
            return "int32";
        else if (type == typeof(long) || type == typeof(long?))
            return "int64";
        else if (type == typeof(float) || type == typeof(float?))
            return "float";
        else if (type == typeof(double) || type == typeof(decimal) || type == typeof(double?) || type == typeof(decimal?))
            return "double";
        else if (type == typeof(DateTime) || type == typeof(DateTime?))
            return "date-time";
        return "string";
    }

    /// <summary>
    /// Determines if a property is nullable.
    /// </summary>
    private static bool IsNullableProperty(PropertyInfo propertyInfo)
    {
        if (!propertyInfo.PropertyType.IsValueType)
            return true; // Reference types are nullable
        return Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null;
    }
}
