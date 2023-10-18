namespace Rystem.NugetHelper.Engine
{
    internal sealed class ApiStrategy : IUpdateStrategy
    {
        public int Value => 1;
        public string Label => "Api";
        public Package GetStrategy()
        {
            var updateTree = new Package()
           .AddProject("Rystem.Api");
            updateTree
                .CreateSon()
                .AddProject("Rystem.Api.Server", "Rystem.Api.Client")
                .CreateSon()
                .AddProject("Rystem.Api.Client.Authentication.BlazorServer", "Rystem.Api.Client.Authentication.BlazorWasm");
            return updateTree;
        }
    }
}
