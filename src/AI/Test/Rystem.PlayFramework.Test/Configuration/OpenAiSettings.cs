namespace Rystem.PlayFramework.Test;

/// <summary>
/// Configuration settings for OpenAI integration.
/// </summary>
public sealed class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string AzureResourceName { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-4o";
    public string Endpoint => $"https://{AzureResourceName}.openai.azure.com/";
}
