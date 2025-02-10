namespace RepositoryFramework
{
    /// <summary>
    /// Common interface for bootstrap of repository or CQRS pattern.
    /// </summary>
    public interface IBootstrapPattern
    {
        ValueTask<bool> BootstrapAsync(CancellationToken cancellationToken = default);
    }
}
