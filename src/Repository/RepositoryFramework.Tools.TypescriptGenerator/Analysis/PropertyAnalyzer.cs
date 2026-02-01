using System.Reflection;
using System.Text.Json.Serialization;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Utils;

namespace RepositoryFramework.Tools.TypescriptGenerator.Analysis;

/// <summary>
/// Analyzes C# properties and extracts PropertyDescriptor.
/// </summary>
public class PropertyAnalyzer
{
    private readonly TypeResolver _typeResolver;

    public PropertyAnalyzer(TypeResolver typeResolver)
    {
        _typeResolver = typeResolver;
    }

    /// <summary>
    /// Analyzes all public properties of a type and returns PropertyDescriptors.
    /// </summary>
    public IReadOnlyList<PropertyDescriptor> AnalyzeProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var descriptors = new List<PropertyDescriptor>(properties.Length);

        foreach (var property in properties)
        {
            // Skip indexers
            if (property.GetIndexParameters().Length > 0)
                continue;

            // Skip properties without getter
            if (!property.CanRead)
                continue;

            var descriptor = AnalyzeProperty(property);
            descriptors.Add(descriptor);
        }

        return descriptors;
    }

    /// <summary>
    /// Analyzes a single property.
    /// </summary>
    public PropertyDescriptor AnalyzeProperty(PropertyInfo property)
    {
        var csharpName = property.Name;
        var jsonName = GetJsonPropertyName(property);
        var typeScriptName = csharpName.ToCamelCase();
        var typeDescriptor = _typeResolver.Resolve(property.PropertyType);

        var isNullable = IsNullableProperty(property);
        var isRequired = IsRequiredProperty(property);

        return new PropertyDescriptor
        {
            CSharpName = csharpName,
            JsonName = jsonName,
            TypeScriptName = typeScriptName,
            Type = typeDescriptor,
            IsRequired = isRequired,
            IsOptional = isNullable || !isRequired
        };
    }

    /// <summary>
    /// Gets the JSON property name from [JsonPropertyName] attribute.
    /// Falls back to the C# property name if not specified.
    /// </summary>
    private static string GetJsonPropertyName(PropertyInfo property)
    {
        var jsonAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (jsonAttribute != null && !string.IsNullOrEmpty(jsonAttribute.Name))
        {
            return jsonAttribute.Name;
        }

        // Check for other common JSON attributes
        // System.Text.Json.Serialization.JsonPropertyNameAttribute is already checked above
        
        // Could also check for Newtonsoft.Json.JsonPropertyAttribute if needed
        // var newtonsoftAttr = property.GetCustomAttributes()
        //     .FirstOrDefault(a => a.GetType().FullName == "Newtonsoft.Json.JsonPropertyAttribute");

        return property.Name;
    }

    /// <summary>
    /// Determines if a property is nullable.
    /// </summary>
    private static bool IsNullableProperty(PropertyInfo property)
    {
        var propertyType = property.PropertyType;

        // Check for Nullable<T>
        if (Nullable.GetUnderlyingType(propertyType) != null)
            return true;

        // Check for nullable reference types using NullabilityInfoContext
        var context = new NullabilityInfoContext();
        var nullabilityInfo = context.Create(property);

        return nullabilityInfo.WriteState == NullabilityState.Nullable ||
               nullabilityInfo.ReadState == NullabilityState.Nullable;
    }

    /// <summary>
    /// Determines if a property is required.
    /// </summary>
    private static bool IsRequiredProperty(PropertyInfo property)
    {
        // Check for [Required] attribute
        var requiredAttr = property.GetCustomAttributes()
            .Any(a => a.GetType().Name == "RequiredAttribute" ||
                      a.GetType().Name == "JsonRequiredAttribute");

        if (requiredAttr)
            return true;

        // Check for required modifier (C# 11+)
        // This is indicated by RequiredMemberAttribute on the type
        // and the property being in the required members list
        var typeRequiredMembers = property.DeclaringType?
            .GetCustomAttributes()
            .FirstOrDefault(a => a.GetType().Name == "RequiredMemberAttribute");

        // For now, we consider non-nullable value types as required
        // and nullable types as optional
        return !IsNullableProperty(property) && property.PropertyType.IsValueType;
    }
}
