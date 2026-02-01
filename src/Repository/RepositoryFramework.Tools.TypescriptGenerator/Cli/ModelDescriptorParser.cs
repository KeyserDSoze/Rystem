using RepositoryFramework.Tools.TypescriptGenerator.Domain;

namespace RepositoryFramework.Tools.TypescriptGenerator.Cli;

/// <summary>
/// Parses the CLI models argument in the format:
/// "{Model,Key,Type,Factory},{Model2,Key2,Type2,Factory2}"
/// Also supports legacy format: [{Model,Key,Type,Factory}]
/// </summary>
public static class ModelDescriptorParser
{
    /// <summary>
    /// Parses the models argument string into a list of RepositoryDescriptor.
    /// Supports two formats:
    /// - New format: "{Model,Key,Type,Factory},{Model2,Key2,Type2,Factory2}"
    /// - Legacy format: [{Model,Key,Type,Factory},{Model2,Key2,Type2,Factory2}]
    /// </summary>
    /// <param name="input">The raw input string from CLI</param>
    /// <returns>List of parsed RepositoryDescriptor</returns>
    /// <exception cref="ArgumentException">Thrown when input format is invalid</exception>
    public static IReadOnlyList<RepositoryDescriptor> Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Models argument cannot be empty.", nameof(input));

        var trimmed = input.Trim();

        // Determine format and get content
        string content;
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            // Legacy format: [{...},{...}]
            content = trimmed[1..^1].Trim();
        }
        else if (trimmed.StartsWith('{'))
        {
            // New format: {...},{...} (already without brackets)
            content = trimmed;
        }
        else
        {
            throw new ArgumentException(
                "Models must start with '{' - Format: \"{Model,Key,Type,Factory},{...}\"",
                nameof(input));
        }

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Models list cannot be empty.", nameof(input));

        // Extract individual blocks {...}
        var blocks = ExtractBlocks(content);

        if (blocks.Count == 0)
            throw new ArgumentException("No valid model descriptors found.", nameof(input));

        // Parse each block
        var descriptors = new List<RepositoryDescriptor>(blocks.Count);
        foreach (var block in blocks)
        {
            descriptors.Add(ParseBlock(block));
        }

        return descriptors;
    }

    /// <summary>
    /// Extracts all {...} blocks from the content string.
    /// Handles nested structures if needed in the future.
    /// </summary>
    private static List<string> ExtractBlocks(string content)
    {
        var blocks = new List<string>();
        var depth = 0;
        var start = -1;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (c == '{')
            {
                if (depth == 0)
                    start = i;
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    blocks.Add(content[start..(i + 1)]);
                    start = -1;
                }
                else if (depth < 0)
                {
                    throw new ArgumentException(
                        $"Unmatched closing brace at position {i}.",
                        nameof(content));
                }
            }
        }

        if (depth != 0)
            throw new ArgumentException(
                "Unmatched opening brace in models argument.",
                nameof(content));

        return blocks;
    }

    /// <summary>
    /// Parses a single block like {Calendar,LeagueKey,Repository,serieA}
    /// </summary>
    private static RepositoryDescriptor ParseBlock(string block)
    {
        // Remove braces
        var content = block.Trim().TrimStart('{').TrimEnd('}').Trim();

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException($"Empty model descriptor: {block}");

        var parts = content.Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length < 3)
            throw new ArgumentException(
                $"Model descriptor must have at least 3 parts (Model,Key,Type): {block}");

        if (parts.Length > 4)
            throw new ArgumentException(
                $"Model descriptor must have at most 4 parts (Model,Key,Type,Factory): {block}");

        var modelName = parts[0];
        var keyName = parts[1];
        var kindString = parts[2];
        var factoryName = parts.Length == 4 ? parts[3] : modelName;

        // Validate model name
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException($"Model name cannot be empty in: {block}");

        // Validate key name
        if (string.IsNullOrWhiteSpace(keyName))
            throw new ArgumentException($"Key name cannot be empty in: {block}");

        // Parse repository kind
        if (!Enum.TryParse<RepositoryKind>(kindString, ignoreCase: true, out var kind))
            throw new ArgumentException(
                $"Invalid repository type '{kindString}' in: {block}. " +
                $"Valid values are: {string.Join(", ", Enum.GetNames<RepositoryKind>())}");

        return new RepositoryDescriptor
        {
            ModelName = modelName,
            KeyName = keyName,
            Kind = kind,
            FactoryName = factoryName
        };
    }

    /// <summary>
    /// Validates the input format and returns a user-friendly error message if invalid.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) Validate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (false, "Models argument is required.");

        try
        {
            Parse(input);
            return (true, null);
        }
        catch (ArgumentException ex)
        {
            return (false, ex.Message);
        }
    }
}
