namespace Rystem.OpenAi.Actors
{
    internal sealed class Scene : IScene
    {
        public string? OpenAiFactoryName { get; set; }
        public string? HttpClientName { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string SimpleActors { get; set; } = string.Empty;
    }
}
