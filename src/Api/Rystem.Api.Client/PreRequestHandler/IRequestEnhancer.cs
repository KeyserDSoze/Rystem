namespace Rystem.Api
{
    public interface IRequestEnhancer
    {
        ValueTask EnhanceAsync(HttpRequestMessage request);
    }
}
