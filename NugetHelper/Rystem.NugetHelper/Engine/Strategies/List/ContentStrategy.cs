namespace Rystem.NugetHelper.Engine
{
    internal sealed class ContentStrategy : IUpdateStrategy
    {
        public int Value => 3;
        public string Label => "Content";
        public Package GetStrategy()
        {
            var onlyContentTree = new Package()
             .AddProject("Rystem.Content.Abstractions");
            onlyContentTree.CreateSon()
                .AddProject("Rystem.Content.Infrastructure.Azure.Storage.Blob",
                        "Rystem.Content.Infrastructure.InMemory",
                        "Rystem.Content.Infrastructure.M365.Sharepoint",
                        "Rystem.Content.Infrastructure.Azure.Storage.File");
            return onlyContentTree;
        }
    }
}
