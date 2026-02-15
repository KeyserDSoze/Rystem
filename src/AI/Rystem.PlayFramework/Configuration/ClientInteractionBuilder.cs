using System.Text.Json;
using System.Text.Json.Nodes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Rystem.PlayFramework.Configuration;

/// <summary>
/// Builder for configuring client-side tool interactions.
/// Used within scene.OnClient() to register tools that execute in browser/mobile app.
/// </summary>
public sealed class ClientInteractionBuilder
{
    private readonly List<ClientInteractionDefinition> _definitions = [];

    /// <summary>
    /// Registers a tool with strongly-typed arguments.
    /// JSON Schema is automatically generated from type T and sent to LLM.
    /// </summary>
    /// <typeparam name="T">Argument model type (must be a class)</typeparam>
    /// <param name="toolName">Unique tool name (e.g., "CapturePhoto")</param>
    /// <param name="description">Human-readable description of what this tool does</param>
    /// <param name="timeoutSeconds">Maximum time client has to execute and return result</param>
    /// <returns>Builder for fluent configuration</returns>
    public ClientInteractionBuilder AddTool<T>(
        string toolName,
        string? description = null,
        int timeoutSeconds = 30) where T : class
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be empty", nameof(toolName));

        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        var jsonSchema = GenerateJsonSchema<T>();

        _definitions.Add(new ClientInteractionDefinition
        {
            ToolName = toolName,
            Description = description,
            TimeoutSeconds = timeoutSeconds,
            ArgumentsType = typeof(T),
            ArgumentsSchema = jsonSchema
        });

        return this;
    }

    /// <summary>
    /// Registers a simple tool without arguments.
    /// Use this for tools that don't need parameters (e.g., "PlaySound", "Vibrate").
    /// </summary>
    /// <param name="toolName">Unique tool name</param>
    /// <param name="description">Human-readable description</param>
    /// <param name="timeoutSeconds">Maximum execution time</param>
    /// <returns>Builder for fluent configuration</returns>
    public ClientInteractionBuilder AddTool(
        string toolName,
        string? description = null,
        int timeoutSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be empty", nameof(toolName));

        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        _definitions.Add(new ClientInteractionDefinition
        {
            ToolName = toolName,
            Description = description,
            TimeoutSeconds = timeoutSeconds,
            ArgumentsType = null,
            ArgumentsSchema = null
        });

        return this;
    }

    /// <summary>
    /// Generates JSON Schema from type T using System.Text.Json introspection.
    /// Supports [Description] and [Range] attributes from System.ComponentModel.
    /// </summary>
    private static string GenerateJsonSchema<T>() where T : class
    {
        var type = typeof(T);
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject(),
            ["required"] = new JsonArray()
        };

        var properties = (JsonObject)schema["properties"]!;
        var required = (JsonArray)schema["required"]!;

        foreach (var prop in type.GetProperties())
        {
            var propSchema = new JsonObject { ["type"] = GetJsonType(prop.PropertyType) };

            // Add description from [Description] attribute
            var descAttr = prop.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() as DescriptionAttribute;
            if (descAttr != null)
                propSchema["description"] = descAttr.Description;

            // Add range constraints from [Range] attribute
            var rangeAttr = prop.GetCustomAttributes(typeof(RangeAttribute), false)
                .FirstOrDefault() as RangeAttribute;
            if (rangeAttr != null)
            {
                if (rangeAttr.Minimum != null)
                    propSchema["minimum"] = Convert.ToInt32(rangeAttr.Minimum);
                if (rangeAttr.Maximum != null)
                    propSchema["maximum"] = Convert.ToInt32(rangeAttr.Maximum);
            }

            // Add default value if property has init accessor with default
            var defaultValue = prop.GetValue(Activator.CreateInstance(type));
            if (defaultValue != null)
                propSchema["default"] = JsonValue.Create(defaultValue);

            properties[ToCamelCase(prop.Name)] = propSchema;

            // Mark as required if not nullable
            if (!IsNullable(prop.PropertyType))
                required.Add(ToCamelCase(prop.Name));
        }

        return schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GetJsonType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
            return "string";
        if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
            underlyingType == typeof(short) || underlyingType == typeof(byte))
            return "integer";
        if (underlyingType == typeof(float) || underlyingType == typeof(double) ||
            underlyingType == typeof(decimal))
            return "number";
        if (underlyingType == typeof(bool))
            return "boolean";
        if (underlyingType.IsArray || (underlyingType.IsGenericType &&
            underlyingType.GetGenericTypeDefinition() == typeof(List<>)))
            return "array";

        return "object";
    }

    private static bool IsNullable(Type type)
    {
        if (!type.IsValueType) return true; // Reference types are nullable
        return Nullable.GetUnderlyingType(type) != null; // Nullable<T>
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;
        return char.ToLowerInvariant(str[0]) + str[1..];
    }

    /// <summary>
    /// Builds the final list of client interaction definitions.
    /// Called internally by SceneBuilder.
    /// </summary>
    internal IReadOnlyList<ClientInteractionDefinition> Build() => _definitions.AsReadOnly();
}
