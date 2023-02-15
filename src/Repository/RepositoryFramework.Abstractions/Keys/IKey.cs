namespace RepositoryFramework
{
    public interface IKey
    {
        string AsString();
        static abstract IKey Parse(string keyAsString);
        internal static string GetStringedValues(params object[] inputs)
            => string.Join('-', inputs.Select(x => x.ToString()));
    }
}
