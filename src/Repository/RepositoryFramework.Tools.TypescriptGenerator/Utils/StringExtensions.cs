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
}
