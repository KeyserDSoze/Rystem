namespace RepositoryFramework
{
    public record struct Key<T1, T2>(T1 Primary, T2 Secondary) : IKey
        where T1 : notnull
        where T2 : notnull
    {
        private static readonly KeySettings<T1> _settings1 = new();
        private static readonly KeySettings<T2> _settings2 = new();
        public static IKey Parse(string keyAsString)
            => new Key<T1, T2>(_settings1.Parse(keyAsString), _settings2.Parse(keyAsString))!;
        public string AsString()
            => IKey.GetStringedValues(Primary, Secondary);
    }
}
