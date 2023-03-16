namespace RepositoryFramework
{
    public record struct Key<T1, T2>(T1 Primary, T2 Secondary) : IKey
        where T1 : notnull
        where T2 : notnull
    {
        public static IKey Parse(string keyAsString)
            => new Key<T1, T2>(KeySettings<T1>.Instance.Parse(keyAsString), KeySettings<T2>.Instance.Parse(keyAsString))!;
        public string AsString()
            => IKey.GetStringedValues(Primary, Secondary);
    }
}
