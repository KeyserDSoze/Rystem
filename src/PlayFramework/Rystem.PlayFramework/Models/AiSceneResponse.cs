namespace Rystem.OpenAi.Actors
{
    public sealed class AiSceneResponse
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Name { get; set; }
        public string? FunctionName { get; set; }
        public string? Message { get; set; }
        public string? Arguments { get; set; }
        public string? Response { get; set; }
    }
}
