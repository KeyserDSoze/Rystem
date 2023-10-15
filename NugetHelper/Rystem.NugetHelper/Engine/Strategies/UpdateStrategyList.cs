namespace Rystem.NugetHelper.Engine
{
    internal sealed class UpdateStrategyList
    {
        public static readonly List<IUpdateStrategy> Updates = new()
        {
            new RystemStrategy(),
            new ApiStrategy(),
            new RepositoryStrategy(),
            new ContentStrategy(),
            new CoreRystemStrategy()
        };
    }
}
