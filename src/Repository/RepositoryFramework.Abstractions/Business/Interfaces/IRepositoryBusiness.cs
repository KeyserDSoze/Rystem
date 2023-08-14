namespace RepositoryFramework
{
    public interface IRepositoryBusiness
    {
        /// <summary>
        /// Parameter used by framework to order the business flow. Lesser priority go first. Same priority override the previous.
        /// </summary>
        int Priority { get; }
    }
}
