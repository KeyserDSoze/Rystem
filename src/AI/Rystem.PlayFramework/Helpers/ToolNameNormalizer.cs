using System.Text.RegularExpressions;

namespace Rystem.PlayFramework.Helpers;

/// <summary>
/// Normalizes tool/function names to comply with OpenAI and other LLM provider requirements.
/// Pattern enforced: ^[a-zA-Z0-9_\.-]+$ (letters, numbers, underscore, dot, hyphen only).
/// </summary>
internal static partial class ToolNameNormalizer
{
    // Compiled regex for better performance with [GeneratedRegex] (.NET 7+)
    [GeneratedRegex(@"[^a-zA-Z0-9_.\-]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"_+")]
    private static partial Regex MultipleUnderscoresRegex();

    /// <summary>
    /// Normalizes a tool name for use with LLM providers.
    /// Replaces invalid characters with underscores, collapses multiple underscores.
    /// </summary>
    /// <param name="name">Original tool/function name</param>
    /// <returns>Normalized name safe for LLM APIs (e.g., "General Requests" → "General_Requests")</returns>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "unnamed_tool";

        // Step 1: Replace any character NOT in [a-zA-Z0-9_.-] with underscore
        var normalized = InvalidCharsRegex().Replace(name, "_");

        // Step 2: Collapse multiple consecutive underscores into one
        normalized = MultipleUnderscoresRegex().Replace(normalized, "_");

        // Step 3: Trim leading/trailing underscores
        normalized = normalized.Trim('_');

        // Step 4: Ensure it doesn't start with a digit (OpenAI rejects this)
        if (normalized.Length > 0 && char.IsDigit(normalized[0]))
        {
            normalized = "tool_" + normalized;
        }

        // Step 5: Fallback if empty after sanitization
        if (string.IsNullOrEmpty(normalized))
        {
            normalized = "tool";
        }

        return normalized;
    }

    /// <summary>
    /// Checks if a normalized name matches an original name.
    /// Use for reverse lookup: find original scene/tool by normalized function name.
    /// </summary>
    /// <param name="originalName">Original name (e.g., "General Requests")</param>
    /// <param name="normalizedName">Normalized name from LLM (e.g., "General_Requests")</param>
    /// <returns>True if they match after normalization</returns>
    public static bool Matches(string? originalName, string? normalizedName)
    {
        if (string.IsNullOrWhiteSpace(originalName) || string.IsNullOrWhiteSpace(normalizedName))
            return false;

        return Normalize(originalName).Equals(normalizedName, StringComparison.OrdinalIgnoreCase);
    }
}
