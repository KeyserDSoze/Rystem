namespace Rystem.PlayFramework.Test;

/// <summary>
/// Configuration settings for OpenAI integration.
/// </summary>
public sealed class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string AzureResourceName { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-4o";
    public string Endpoint { get; set; } = string.Empty;

    public string GetEndpoint()
    {
        if (!string.IsNullOrWhiteSpace(Endpoint))
            return Endpoint;

        return string.IsNullOrWhiteSpace(AzureResourceName)
            ? string.Empty
            : $"https://{AzureResourceName}.openai.azure.com/";
    }
}
