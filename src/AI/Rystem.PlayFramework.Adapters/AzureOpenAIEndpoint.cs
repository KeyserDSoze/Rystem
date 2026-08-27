namespace Rystem.PlayFramework.Adapters;

internal static class AzureOpenAIEndpoint
{
    public static Uri Normalize(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (!endpoint.IsAbsoluteUri)
            throw new ArgumentException("The Azure OpenAI endpoint must be an absolute URI.", nameof(endpoint));

        if (!string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new ArgumentException("The Azure OpenAI endpoint must not contain a query string or fragment.", nameof(endpoint));

        var isHttps = endpoint.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        var isLocalHttp = endpoint.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && endpoint.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        if (!isHttps && !isLocalHttp)
            throw new ArgumentException("The Azure OpenAI endpoint must use HTTPS. HTTP is allowed only for localhost.", nameof(endpoint));

        var segments = endpoint.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (segments[index].Equals("openai", StringComparison.OrdinalIgnoreCase)
                && segments[index + 1].Equals("deployments", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Deployment-specific Azure OpenAI endpoints are not supported. Use the resource endpoint instead.",
                    nameof(endpoint));
            }
        }

        var v1Index = Array.FindIndex(segments, segment => segment.Equals("v1", StringComparison.OrdinalIgnoreCase));
        if (v1Index >= 0)
        {
            if (v1Index != segments.Length - 1)
            {
                throw new ArgumentException(
                    "The Azure OpenAI endpoint must not have additional path segments after 'v1'. " +
                    "Use the resource endpoint or an endpoint terminating in '/v1'.",
                    nameof(endpoint));
            }

            return endpoint;
        }

        var builder = new UriBuilder(endpoint);
        var path = endpoint.AbsolutePath.TrimEnd('/');
        builder.Path = path.EndsWith("/openai", StringComparison.OrdinalIgnoreCase)
            ? $"{path}/v1"
            : $"{path}/openai/v1";

        return builder.Uri;
    }
}
