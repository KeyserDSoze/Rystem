namespace System.Population.Random
{
    public sealed class RandomPopulationFromRystemSettings
    {
        public bool UseTheSameRandomValuesForTheSameType { get; set; }
        public required Type StartingType { get; set; }
        public Func<dynamic>? Creator { get; set; }
        public string? ForcedKey { get; set; }
    }
}
