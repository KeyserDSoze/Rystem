namespace RepositoryFramework
{
    public record struct Key<T1, T2, T3, T4, T5>(T1 Primary, T2 Secondary, T3 Tertiary, T4 Quaternary, T5 Quinary) : IKey
       where T1 : notnull
       where T2 : notnull
       where T3 : notnull
       where T4 : notnull
       where T5 : notnull
    {
        private static readonly KeySettings<T1> _settings1 = new();
        private static readonly KeySettings<T2> _settings2 = new();
        private static readonly KeySettings<T3> _settings3 = new();
        private static readonly KeySettings<T4> _settings4 = new();
        private static readonly KeySettings<T5> _settings5 = new();
        public static IKey Parse(string keyAsString)
            => new Key<T1, T2, T3, T4, T5>(_settings1.Parse(keyAsString), _settings2.Parse(keyAsString), _settings3.Parse(keyAsString), _settings4.Parse(keyAsString), _settings5.Parse(keyAsString))!;
        public string AsString()
            => IKey.GetStringedValues(Primary, Secondary, Tertiary, Quaternary, Quinary);
    }
}
