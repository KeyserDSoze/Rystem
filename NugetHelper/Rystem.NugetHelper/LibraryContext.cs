namespace Rystem.NugetHelper
{
    internal enum VersionType
    {
        Major,
        Minor,
        Patch,
        Specific
    }
    internal record Version
    {
        public Version(string v)
        {
            V = v;
        }
        public int Major => int.Parse(V.Split('.').First());
        public int Minor => int.Parse(V.Split('.').Skip(1).First());
        public int Patch => int.Parse(V.Split('.').Skip(2).First());

        public string V { get; private set; }

        public bool IsGreater(Version context)
        {
            if (context.Major > Major)
                return true;
            else if (context.Major == Major)
            {
                if (context.Minor > Minor)
                    return true;
                else if (context.Minor == Minor)
                    return context.Patch > Patch;
            }
            return false;
        }
        public void NextVersion(VersionType type, int addingValue)
        {
            switch (type)
            {
                case VersionType.Major:
                    V = $"{Major + addingValue}.0.0";
                    break;
                case VersionType.Minor:
                    V = $"{Major}.{Minor + addingValue}.0";
                    break;
                case VersionType.Patch:
                    V = $"{Major}.{Minor}.{Patch + addingValue}";
                    break;
            }
        }
        public void SetVersion(VersionType type, string newVersion)
        {
            if (type == VersionType.Specific)
                V = newVersion;
        }
    }
    internal record LibraryContext
    {
        public string? LibraryName { get; set; }
        public string NormalizedName => LibraryName!.Replace("Rystem.RepositoryFramework.", "RepositoryFramework.");
        public List<string> RepoToUpdate { get; set; } = new();
        public LibraryContext(string version = "0.0.0")
        {
            Version = new(version);
        }
        public Version Version { get; internal set; }
    }
}
