namespace RepositoryFramework.Tools.TypescriptGenerator.Utils;

/// <summary>
/// String extension methods for naming conventions.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to camelCase.
    /// Example: "UserProfile" -> "userProfile"
    /// </summary>
    public static string ToCamelCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length == 1)
            return value.ToLowerInvariant();

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// Example: "userProfile" -> "UserProfile"
    /// </summary>
    public static string ToPascalCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length == 1)
            return value.ToUpperInvariant();

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// Converts a string to kebab-case.
    /// Example: "UserProfile" -> "user-profile"
    /// </summary>
    public static string ToKebabCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new System.Text.StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                    result.Append('-');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a C# class name to a TypeScript file name.
    /// Example: "UserProfile" -> "user-profile.ts"
    /// </summary>
    public static string ToTypeScriptFileName(this string className)
        => $"{className.ToKebabCase()}.ts";

    /// <summary>
    /// Converts a C# class name to a TypeScript service file name.
    /// Example: "UserProfile" -> "user-profile.service.ts"
    /// </summary>
    public static string ToServiceFileName(this string className)
        => $"{className.ToKebabCase()}.service.ts";

    /// <summary>
    /// Strips namespaces from a type name, correctly handling generic type arguments.
    /// Unlike a simple LastIndexOf('.'), this method respects angle bracket nesting.
    /// <para>"Namespace.EntityVersions&lt;Namespace.Paragraph&gt;" → "EntityVersions&lt;Paragraph&gt;"</para>
    /// <para>"Namespace.Calendar" → "Calendar"</para>
    /// <para>"Calendar" → "Calendar"</para>
    /// </summary>
    public static string GetSimpleTypeName(this string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return fullName;

        var ltIndex = fullName.IndexOf('<');

        if (ltIndex < 0)
        {
            // Non-generic: just strip namespace
            var lastDot = fullName.LastIndexOf('.');
            return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
        }

        // Generic: "Namespace.EntityVersions<Namespace.Paragraph, Namespace.Location>"
        var basePart = fullName[..ltIndex];
        var argsPart = fullName[(ltIndex + 1)..^1];

        // Strip namespace from base
        var baseLastDot = basePart.LastIndexOf('.');
        var simpleBase = baseLastDot >= 0 ? basePart[(baseLastDot + 1)..] : basePart;

        // Recursively simplify each type argument
        var args = SplitGenericArgs(argsPart);
        var simpleArgs = args.Select(a => a.Trim().GetSimpleTypeName());

        return $"{simpleBase}<{string.Join(", ", simpleArgs)}>";
    }

    /// <summary>
    /// Converts a C# type name to the CLR-style path format used by the Rystem API server.
    /// Matches the server logic: <c>$"{modelType.Name}{string.Join('_', modelType.GetGenericArguments().Select(x => x.Name))}"</c>
    /// <para>"EntityVersions&lt;Paragraph&gt;" → "EntityVersions`1Paragraph"</para>
    /// <para>"Entity&lt;Paragraph, Location&gt;" → "Entity`2Paragraph_Location"</para>
    /// <para>"Calendar" → "Calendar" (non-generic, unchanged)</para>
    /// </summary>
    public static string ToClrStyleApiPath(this string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return typeName;

        var simpleName = typeName.GetSimpleTypeName();

        var ltIndex = simpleName.IndexOf('<');
        if (ltIndex < 0)
            return simpleName;

        var baseName = simpleName[..ltIndex];
        var argsPart = simpleName[(ltIndex + 1)..^1];
        var args = SplitGenericArgs(argsPart).Select(a => a.Trim()).ToList();

        return $"{baseName}`{args.Count}{string.Join("_", args)}";
    }

    /// <summary>
    /// Splits comma-separated generic type arguments, respecting angle bracket nesting.
    /// <para>"Paragraph, Location" → ["Paragraph", "Location"]</para>
    /// <para>"EntityVersions&lt;Book&gt;, string" → ["EntityVersions&lt;Book&gt;", "string"]</para>
    /// </summary>
    public static List<string> SplitGenericArgs(string args)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == '<') depth++;
            else if (args[i] == '>') depth--;
            else if (args[i] == ',' && depth == 0)
            {
                result.Add(args[start..i]);
                start = i + 1;
            }
        }

        result.Add(args[start..]);
        return result;
    }
}
