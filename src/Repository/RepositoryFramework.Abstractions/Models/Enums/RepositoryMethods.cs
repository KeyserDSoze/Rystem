namespace RepositoryFramework
{
    /// <summary>
    /// Mapping for methods in repository pattern or CQRS.
    /// </summary>
    [Flags]
    public enum RepositoryMethods
    {
        Insert = 1,
        Update = 2,
        Delete = 4,
        Batch = 8,
        Exist = 16,
        Get = 32,
        Query = 64,
        Operation = 128,
        All = 256,
    }
}
