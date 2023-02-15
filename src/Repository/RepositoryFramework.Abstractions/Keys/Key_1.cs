namespace RepositoryFramework
{
    public record struct Key<T1>(T1 Primary) : IKey
        where T1 : notnull
    {
        private static readonly KeySettings<T1> _settings1 = new();
        public static IKey Parse(string keyAsString) 
            => new Key<T1>(_settings1.Parse(keyAsString))!;
        public string AsString()
            => IKey.GetStringedValues(Primary);
    }
}
