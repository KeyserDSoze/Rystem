using System.Text;
using System.Text.RegularExpressions;

namespace RepositoryFramework.Tools.TypescriptGenerator.Utils;

/// <summary>
/// Helper class for parsing and normalizing generic type names.
/// Supports both user-friendly syntax (EntityVersions&lt;Timeline&gt;) and 
/// .NET reflection syntax (EntityVersions`1[[Namespace.Timeline]]).
/// </summary>
public static partial class GenericTypeHelper
{
    /// <summary>
    /// Checks if a type name contains generic parameters.
    /// </summary>
    public static bool IsGenericType(string typeName)
    {
        return typeName.Contains('<') || typeName.Contains('`');
    }

    /// <summary>
    /// Parses a generic type name and returns its components.
    /// Supports both syntaxes:
    /// - User-friendly: EntityVersions&lt;Timeline&gt;
    /// - Reflection: EntityVersions`1[[GhostWriter.Business.Timeline]]
    /// </summary>
    public static GenericTypeInfo Parse(string typeName)
    {
        if (!IsGenericType(typeName))
        {
            return new GenericTypeInfo
            {
                BaseTypeName = typeName,
                TypeArguments = [],
                IsGeneric = false
            };
        }

        // Try user-friendly syntax first: EntityVersions<Timeline>
        if (typeName.Contains('<'))
        {
            return ParseUserFriendly(typeName);
        }

        // Otherwise, parse reflection syntax: EntityVersions`1[[Timeline]]
        return ParseReflection(typeName);
    }

    /// <summary>
    /// Parses user-friendly generic syntax: EntityVersions&lt;Timeline&gt;
    /// </summary>
    private static GenericTypeInfo ParseUserFriendly(string typeName)
    {
        var openBracket = typeName.IndexOf('<');
        var closeBracket = typeName.LastIndexOf('>');

        if (openBracket == -1 || closeBracket == -1)
        {
            throw new ArgumentException($"Invalid generic type syntax: {typeName}");
        }

        var baseType = typeName[..openBracket].Trim();
        var argsString = typeName[(openBracket + 1)..closeBracket].Trim();

        var typeArgs = SplitTypeArguments(argsString);

        return new GenericTypeInfo
        {
            BaseTypeName = baseType,
            TypeArguments = typeArgs,
            IsGeneric = true
        };
    }

    /// <summary>
    /// Parses .NET reflection syntax: EntityVersions`1[[GhostWriter.Business.Timeline]]
    /// </summary>
    private static GenericTypeInfo ParseReflection(string typeName)
    {
        var backtickIndex = typeName.IndexOf('`');
        if (backtickIndex == -1)
        {
            throw new ArgumentException($"Invalid reflection generic type syntax: {typeName}");
        }

        var baseType = typeName[..backtickIndex].Trim();

        // Extract type arguments from [[...]]
        var pattern = @"\[\[([^\]]+)\]\]";
        var matches = Regex.Matches(typeName, pattern);

        var typeArgs = new List<string>();
        foreach (Match match in matches)
        {
            var fullName = match.Groups[1].Value.Trim();

            // Extract just the type name without assembly info
            // "GhostWriter.Business.Timeline, Assembly" -> "GhostWriter.Business.Timeline"
            var commaIndex = fullName.IndexOf(',');
            var typeName2 = commaIndex > 0 ? fullName[..commaIndex].Trim() : fullName;

            typeArgs.Add(typeName2);
        }

        return new GenericTypeInfo
        {
            BaseTypeName = baseType,
            TypeArguments = typeArgs,
            IsGeneric = true
        };
    }

    /// <summary>
    /// Splits comma-separated type arguments, respecting nested generics.
    /// Example: "List&lt;string&gt;, Dictionary&lt;string, int&gt;" -> ["List&lt;string&gt;", "Dictionary&lt;string, int&gt;"]
    /// </summary>
    private static List<string> SplitTypeArguments(string argsString)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var depth = 0;

        foreach (var c in argsString)
        {
            if (c == '<')
            {
                depth++;
                current.Append(c);
            }
            else if (c == '>')
            {
                depth--;
                current.Append(c);
            }
            else if (c == ',' && depth == 0)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString().Trim());
        }

        return result;
    }

    /// <summary>
    /// Converts a user-friendly generic type name to .NET reflection format
    /// for use with Type.GetType().
    /// EntityVersions&lt;Timeline&gt; -> EntityVersions`1
    /// </summary>
    public static string ToReflectionTypeName(string typeName)
    {
        if (!IsGenericType(typeName))
            return typeName;

        var info = Parse(typeName);
        if (!info.IsGeneric)
            return typeName;

        // Build reflection name: BaseType`N
        return $"{info.BaseTypeName}`{info.TypeArguments.Count}";
    }

    /// <summary>
    /// Converts to user-friendly format for display and TypeScript generation.
    /// EntityVersions`1[[Timeline]] -> EntityVersions&lt;Timeline&gt;
    /// </summary>
    public static string ToUserFriendlyName(string typeName)
    {
        if (!IsGenericType(typeName))
            return typeName;

        var info = Parse(typeName);
        if (!info.IsGeneric || info.TypeArguments.Count == 0)
            return typeName;

        // Extract simple names from type arguments
        var simpleArgs = info.TypeArguments.Select(GetSimpleName);
        return $"{info.BaseTypeName}<{string.Join(", ", simpleArgs)}>";
    }

    /// <summary>
    /// Gets the simple name from a fully qualified type name.
    /// "GhostWriter.Business.Timeline" -> "Timeline"
    /// </summary>
    private static string GetSimpleName(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }
}

/// <summary>
/// Information about a parsed generic type.
/// </summary>
public sealed record GenericTypeInfo
{
    /// <summary>
    /// The base type name without generic parameters.
    /// Example: "EntityVersions" from "EntityVersions&lt;Timeline&gt;"
    /// </summary>
    public required string BaseTypeName { get; init; }

    /// <summary>
    /// The list of type arguments.
    /// Example: ["Timeline"] from "EntityVersions&lt;Timeline&gt;"
    /// </summary>
    public required List<string> TypeArguments { get; init; }

    /// <summary>
    /// Whether this is a generic type.
    /// </summary>
    public required bool IsGeneric { get; init; }

    /// <summary>
    /// Gets the user-friendly display name.
    /// Example: "EntityVersions&lt;Timeline&gt;"
    /// </summary>
    public string DisplayName =>
        IsGeneric && TypeArguments.Count > 0
            ? $"{BaseTypeName}<{string.Join(", ", TypeArguments.Select(GetSimpleName))}>"
            : BaseTypeName;

    /// <summary>
    /// Gets the reflection-compatible name for Type.GetType().
    /// Example: "EntityVersions`1"
    /// </summary>
    public string ReflectionName =>
        IsGeneric
            ? $"{BaseTypeName}`{TypeArguments.Count}"
            : BaseTypeName;

    private static string GetSimpleName(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }
}
