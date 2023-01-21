namespace RepositoryFramework
{
    public record struct Key<T1>(T1 Primary) : IKey
        where T1 : notnull
    {
        public string AsString()
            => IKey.GetStringedValues(Primary);
    }
}
