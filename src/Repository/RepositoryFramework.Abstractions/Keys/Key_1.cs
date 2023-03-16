namespace RepositoryFramework
{
    public record struct Key<T1>(T1 Primary) : IKey
        where T1 : notnull
    {
        public static IKey Parse(string keyAsString) 
            => new Key<T1>(KeySettings<T1>.Instance.Parse(keyAsString))!;
        public string AsString()
            => IKey.GetStringedValues(Primary);
    }
}
