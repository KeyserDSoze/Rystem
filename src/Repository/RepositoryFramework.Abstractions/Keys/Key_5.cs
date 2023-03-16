namespace RepositoryFramework
{
    public record struct Key<T1, T2, T3, T4, T5>(T1 Primary, T2 Secondary, T3 Tertiary, T4 Quaternary, T5 Quinary) : IKey
       where T1 : notnull
       where T2 : notnull
       where T3 : notnull
       where T4 : notnull
       where T5 : notnull
    {
        public static IKey Parse(string keyAsString)
            => new Key<T1, T2, T3, T4, T5>(KeySettings<T1>.Instance.Parse(keyAsString),
                KeySettings<T2>.Instance.Parse(keyAsString),
                KeySettings<T3>.Instance.Parse(keyAsString),
                KeySettings<T4>.Instance.Parse(keyAsString),
                KeySettings<T5>.Instance.Parse(keyAsString))!;
        public string AsString()
            => IKey.GetStringedValues(Primary, Secondary, Tertiary, Quaternary, Quinary);
    }
}
