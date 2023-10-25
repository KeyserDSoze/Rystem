namespace Rystem.Api
{
    public interface IRequestEnhancer
    {
        ValueTask EnhanceAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }
}
