namespace Rystem.NugetHelper.Engine
{
    internal sealed class CoreRystemStrategy : IUpdateStrategy
    {
        public int Value => 4;
        public string Label => "Core";
        public Package GetStrategy()
        {
            var onlyRystemTree = new Package()
               .AddProject("Rystem.DependencyInjection");
            onlyRystemTree
                .CreateSon()
                    .AddProject("Rystem.Concurrency")
                .CreateSon()
                    .AddProject("Rystem.BackgroundJob")
                .CreateSon()
                    .AddProject("Rystem.Queue");
            return onlyRystemTree;
        }
    }
}
