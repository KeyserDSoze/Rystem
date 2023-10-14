using System.Text;

namespace Rystem.Api
{
    public sealed class ApiClientRequestBearer
    {
        public StringBuilder? Path { get; set; }
        public StringBuilder? Query { get; set; }
        public StringBuilder? Cookie { get; set; }
        public HttpContent? Content { get; set; }
        public Dictionary<string, string?> Headers { get; set; } = new();
    }
}
