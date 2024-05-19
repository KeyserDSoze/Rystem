namespace Rystem.NugetHelper.Engine
{
    internal sealed class AuthenticationStrategy : IUpdateStrategy
    {
        public int Value => 1;
        public string Label => "Authentication";
        public Package GetStrategy()
        {
            var updateTree = new Package().AddProject("Rystem.Authentication.Social.Abstractions");
            updateTree.CreateSon()
                .AddProject("Rystem.Authentication.Social", "rystem.authentication.social.react", "Rystem.Authentication.Social.Blazor");
            return updateTree;
        }
    }
}
