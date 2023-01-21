namespace RepositoryFramework
{
    public record struct Key<T1, T2>(T1 Primary, T2 Secondary) : IKey
        where T1 : notnull
        where T2 : notnull
    {
        public string AsString()
            => IKey.GetStringedValues(Primary, Secondary);
    }
}
