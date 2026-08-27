using Rystem.PlayFramework.Adapters;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class AzureOpenAIEndpointTests
{
    [Theory]
    [InlineData("https://name.openai.azure.com", "https://name.openai.azure.com/openai/v1")]
    [InlineData("https://name.openai.azure.com/", "https://name.openai.azure.com/openai/v1")]
    [InlineData("https://name.openai.azure.com/openai", "https://name.openai.azure.com/openai/v1")]
    [InlineData("https://name.openai.azure.com/openai/", "https://name.openai.azure.com/openai/v1")]
    [InlineData("https://name.openai.azure.com/openai/v1", "https://name.openai.azure.com/openai/v1")]
    [InlineData("https://name.openai.azure.com/openai/v1/", "https://name.openai.azure.com/openai/v1/")]
    [InlineData("https://custom.example/api/v1", "https://custom.example/api/v1")]
    [InlineData("https://custom.example/api/v1/", "https://custom.example/api/v1/")]
    [InlineData("http://localhost:5272", "http://localhost:5272/openai/v1")]
    [InlineData("https://NAME.openai.azure.com/OpenAI", "https://name.openai.azure.com/OpenAI/v1")]
    public void Normalize_ReturnsExpectedEndpoint(string input, string expected)
    {
        var actual = AzureOpenAIEndpoint.Normalize(new Uri(input));

        Assert.Equal(expected, actual.AbsoluteUri);
    }

    [Theory]
    [InlineData("https://name.openai.azure.com/openai/deployments/my-deployment")]
    [InlineData("http://name.openai.azure.com")]
    [InlineData("ftp://name.openai.azure.com")]
    [InlineData("https://name.openai.azure.com?api-version=preview")]
    [InlineData("https://name.openai.azure.com/#fragment")]
    [InlineData("https://host/openai/v1/proxy")]
    [InlineData("https://host/v1/extra/segment")]
    public void Normalize_RejectsUnsupportedEndpoint(string input)
    {
        Assert.Throws<ArgumentException>(() => AzureOpenAIEndpoint.Normalize(new Uri(input)));
    }

    [Fact]
    public void Normalize_RejectsRelativeUri()
    {
        Assert.Throws<ArgumentException>(() => AzureOpenAIEndpoint.Normalize(new Uri("openai/v1", UriKind.Relative)));
    }
}
