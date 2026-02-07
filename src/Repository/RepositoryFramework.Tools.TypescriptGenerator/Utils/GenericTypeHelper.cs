using System.Text;

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
    /// - Open generic (no args): EntityVersions`1 -> treated as non-generic base type
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

        // Check if this is reflection syntax with type arguments: EntityVersions`1[[...]]
        // or just an open generic definition: EntityVersions`1
        if (!typeName.Contains("[["))
        {
            // This is an open generic type (e.g., EntityVersions`1 without type arguments)
            // Treat it as the base type name, not a generic to instantiate
            return new GenericTypeInfo
            {
                BaseTypeName = typeName,
                TypeArguments = [],
                IsGeneric = false // Important: open generics are NOT considered "generic" for our purposes
            };
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
    /// Uses iterative bracket-counting parser to avoid regex catastrophic backtracking
    /// with deeply nested generics and assembly-qualified names.
    /// </summary>
    private static GenericTypeInfo ParseReflection(string typeName)
    {
        var backtickIndex = typeName.IndexOf('`');
        if (backtickIndex == -1)
        {
            throw new ArgumentException($"Invalid reflection generic type syntax: {typeName}");
        }

        var baseType = typeName[..backtickIndex].Trim();

        // Extract type arguments using iterative bracket counting (O(n) instead of exponential regex)
        var typeArgs = ExtractTypeArgumentsIterative(typeName);

        return new GenericTypeInfo
        {
            BaseTypeName = baseType,
            TypeArguments = typeArgs,
            IsGeneric = true
        };
    }

    /// <summary>
    /// Extracts type arguments from reflection syntax using bracket counting.
    /// Handles nested generics like: EntityVersions`1[[EntityVersion`1[[Book, Assembly]], Assembly]]
    /// without catastrophic backtracking. Time complexity: O(n).
    /// </summary>
    private static List<string> ExtractTypeArgumentsIterative(string typeName)
    {
        var typeArgs = new List<string>();
        var length = typeName.Length;
        var i = 0;

        // Find the start of type arguments (after `N part)
        while (i < length && typeName[i] != '[')
        {
            i++;
        }

        // Process each type argument enclosed in [[...]]
        while (i < length)
        {
            // Skip whitespace
            while (i < length && char.IsWhiteSpace(typeName[i]))
            {
                i++;
            }

            // Look for opening [[
            if (i >= length - 1 || typeName[i] != '[' || typeName[i + 1] != '[')
            {
                break;
            }

            i += 2; // Skip [[

            // Find matching ]] using bracket counter
            var depth = 1; // We're inside one level
            var start = i;

            while (i < length && depth > 0)
            {
                if (i < length - 1 && typeName[i] == '[' && typeName[i + 1] == '[')
                {
                    depth++;
                    i += 2;
                }
                else if (i < length - 1 && typeName[i] == ']' && typeName[i + 1] == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        // Found matching ]]
                        var fullName = typeName[start..i].Trim();

                        // Extract just the type name without assembly info
                        // "GhostWriter.Business.Timeline, Assembly, Version=..." -> "GhostWriter.Business.Timeline"
                        // BUT: Respect nested brackets! "EntityVersion`1[[Book, Assembly]], OuterAssembly" 
                        // should extract "EntityVersion`1[[Book, Assembly]]", not "EntityVersion`1[[Book"
                        var cleanTypeName = ExtractTypeNameRespectingBrackets(fullName);

                        typeArgs.Add(cleanTypeName);
                    }
                    i += 2;
                }
                else
                {
                    i++;
                }
            }

            // Skip comma separator between type arguments
            while (i < length && (typeName[i] == ',' || char.IsWhiteSpace(typeName[i])))
            {
                i++;
            }
        }

        return typeArgs;
    }

    /// <summary>
    /// Extracts the type name from a string that may contain assembly information,
    /// respecting nested bracket structures and recursively cleaning nested assembly info.
    /// Examples:
    /// - "GhostWriter.Core.Book, Assembly" -> "GhostWriter.Core.Book"
    /// - "EntityVersion`1[[Book, Asm]], OuterAsm" -> "EntityVersion`1[[Book]]"
    /// </summary>
    private static string ExtractTypeNameRespectingBrackets(string fullName)
    {
        // First, find where the type name ends (at the first comma outside all brackets)
        var bracketDepth = 0;
        var typeNameEnd = fullName.Length;

        for (var i = 0; i < fullName.Length; i++)
        {
            var ch = fullName[i];

            // Track bracket depth
            if (ch == '[')
            {
                bracketDepth++;
            }
            else if (ch == ']')
            {
                bracketDepth--;
            }
            // Only consider commas OUTSIDE all brackets
            else if (ch == ',' && bracketDepth == 0)
            {
                // Found the assembly separator
                typeNameEnd = i;
                break;
            }
        }

        var typeName = fullName[..typeNameEnd].Trim();

        // Now recursively clean nested brackets [[...]]
        // Replace [[X, Assembly]] with [[X]]
        return CleanNestedAssemblyInfo(typeName);
    }

    /// <summary>
    /// Recursively removes assembly information from nested generic type arguments.
    /// EntityVersion`1[[Book, GhostWriter.Core]] -> EntityVersion`1[[Book]]
    /// </summary>
    private static string CleanNestedAssemblyInfo(string typeName)
    {
        // If no brackets, nothing to clean
        if (!typeName.Contains("[["))
            return typeName;

        var result = new StringBuilder();
        var i = 0;
        var length = typeName.Length;

        while (i < length)
        {
            // Look for [[ opening
            if (i < length - 1 && typeName[i] == '[' && typeName[i + 1] == '[')
            {
                result.Append("[[");
                i += 2;

                // Find the matching ]]
                var depth = 1;
                var start = i;

                while (i < length && depth > 0)
                {
                    if (i < length - 1 && typeName[i] == '[' && typeName[i + 1] == '[')
                    {
                        depth++;
                        i += 2;
                    }
                    else if (i < length - 1 && typeName[i] == ']' && typeName[i + 1] == ']')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            // Extract content and clean it recursively
                            var content = typeName[start..i];
                            var cleaned = ExtractTypeNameRespectingBrackets(content);
                            result.Append(cleaned);
                            result.Append("]]");
                            i += 2;
                        }
                        else
                        {
                            i += 2;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            else
            {
                result.Append(typeName[i]);
                i++;
            }
        }

        return result.ToString();
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
