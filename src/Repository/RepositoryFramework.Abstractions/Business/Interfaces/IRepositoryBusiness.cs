namespace RepositoryFramework
{
    public interface IRepositoryBusiness
    {
        /// <summary>
        /// Parameter used by framework to order the business flow. Lesser priority go first.
        /// </summary>
        int Priority { get; }
    }
}
