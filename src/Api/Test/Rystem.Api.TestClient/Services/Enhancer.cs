
namespace Rystem.Api.TestClient.Services
{
    public class Enhancer : IRequestEnhancer
    {
        public ValueTask EnhanceAsync(HttpRequestMessage request)
        {
            return ValueTask.CompletedTask;
        }
    }
    public class Enhancer2 : IRequestEnhancer
    {
        public ValueTask EnhanceAsync(HttpRequestMessage request)
        {
            return ValueTask.CompletedTask;
        }
    }
    public class Enhancer3 : IRequestEnhancer
    {
        public ValueTask EnhanceAsync(HttpRequestMessage request)
        {
            return ValueTask.CompletedTask;
        }
    }
    public class Enhancer4 : IRequestEnhancer
    {
        public ValueTask EnhanceAsync(HttpRequestMessage request)
        {
            return ValueTask.CompletedTask;
        }
    }
}
