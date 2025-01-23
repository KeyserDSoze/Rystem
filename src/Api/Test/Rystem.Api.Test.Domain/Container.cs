namespace Rystem.Api.Test.Domain
{
    public sealed class Container
    {
        public required string Name { get; set; }
        public required ContainerType Type { get; set; }
        public string? Prompt { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = [];
    }
}
