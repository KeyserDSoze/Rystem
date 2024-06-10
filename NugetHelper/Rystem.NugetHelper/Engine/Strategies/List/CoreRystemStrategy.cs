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
                    .AddProject("Rystem.Concurrency", "Rystem.DependencyInjection.Web")
                .CreateSon()
                    .AddProject("Rystem.BackgroundJob", "Rystem.Concurrency.Redis", "Rystem.Test.XUnit")
                .CreateSon()
                    .AddProject("Rystem.Queue");
            return onlyRystemTree;
        }
    }
}
