using Microsoft.Extensions.Configuration;

namespace Rystem.PlayFramework.Test;

internal static class AzureLiveTestConfiguration
{
    private const string SecretPlaceholder = "in secrets";

    private static readonly IReadOnlyDictionary<string, string> ConfigurationKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AZURE_OPENAI_ENDPOINT"] = "OpenAi:Endpoint",
            ["AZURE_OPENAI_API_KEY"] = "OpenAi:ApiKey",
            ["AZURE_OPENAI_DEPLOYMENT"] = "OpenAi:Deployments:Responses",
            ["AZURE_OPENAI_CHAT_DEPLOYMENT"] = "OpenAi:Deployments:Chat",
            ["AZURE_OPENAI_FILES_DEPLOYMENT"] = "OpenAi:Deployments:Files",
            ["AZURE_OPENAI_VISION_DEPLOYMENT"] = "OpenAi:Deployments:Vision",
            ["AZURE_OPENAI_AUDIO_DEPLOYMENT"] = "OpenAi:Deployments:Audio",
            ["AZURE_OPENAI_STT_DEPLOYMENT"] = "OpenAi:Deployments:SpeechToText",
            ["AZURE_OPENAI_TTS_DEPLOYMENT"] = "OpenAi:Deployments:TextToSpeech"
        };

    public static IConfiguration BuildConfiguration() => new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddUserSecrets<PlayFrameworkTestBase>()
        .Build();

    public static string? Resolve(string environmentVariable, IConfiguration? configuration = null)
    {
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariable);
        if (IsConfigured(environmentValue))
            return environmentValue;

        configuration ??= BuildConfiguration();
        if (!ConfigurationKeys.TryGetValue(environmentVariable, out var key))
            return null;

        var configuredValue = configuration[key];
        return IsConfigured(configuredValue) ? configuredValue : null;
    }

    private static bool IsConfigured(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Equals(SecretPlaceholder, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Shared skip-reason computation for <see cref="AzureLiveFactAttribute"/> and
    /// <see cref="AzureLiveTheoryAttribute"/>: opts the test out unless the live-run flag is set and
    /// every value required by the eventual <see cref="PlayFrameworkTestBase"/> constructor call is
    /// configured, so an incomplete setup produces a skipped test instead of a failed one.
    /// </summary>
    public static string? ComputeSkipReason(
        bool requiresApiKey,
        bool requiresDefaultDeployment,
        IReadOnlyCollection<string> requiredEnvironmentVariables)
    {
        if (Environment.GetEnvironmentVariable("RYSTEM_RUN_AZURE_OPENAI_INTEGRATION") != "1")
            return "Set RYSTEM_RUN_AZURE_OPENAI_INTEGRATION=1 to run Azure live tests.";

        var configuration = BuildConfiguration();

        var required = new List<string> { "AZURE_OPENAI_ENDPOINT" };
        if (requiresApiKey)
            required.Add("AZURE_OPENAI_API_KEY");
        if (requiresDefaultDeployment)
            required.Add("AZURE_OPENAI_DEPLOYMENT");
        required.AddRange(requiredEnvironmentVariables);

        var missing = required
            .Distinct(StringComparer.Ordinal)
            .Where(name => Resolve(name, configuration) is null)
            .ToArray();

        return missing.Length > 0
            ? $"Set {string.Join(", ", missing)} as environment variables or matching OpenAi:Deployments/OpenAi user secrets."
            : null;
    }
}
