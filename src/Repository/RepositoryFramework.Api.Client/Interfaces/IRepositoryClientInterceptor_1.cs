namespace RepositoryFramework.Api.Client
{
    /// <summary>
    /// Interface for specific interceptor request for your repository or CQRS client of <typeparamref name="T"/> and <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="T">Model used for your repository</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed", Justification = "It's not used but it's needed for the return methods that use this class.")]
    public interface IRepositoryClientInterceptor<T> : IRepositoryClientInterceptor
    {
    }
}