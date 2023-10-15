namespace Rystem.NugetHelper.Engine
{
    internal class Package
    {
        public List<LibraryContext> Libraries { get; set; } = new();
        public Package? Son { get; set; }
        public Package CreateSon()
            => Son = new Package();
        public Package AddProject(params string[] projectNames)
        {
            Libraries.AddRange(projectNames.Select(x => new LibraryContext { LibraryName = x }));
            return this;
        }
    }
}
