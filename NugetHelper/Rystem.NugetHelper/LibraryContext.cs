namespace Rystem.NugetHelper
{
    internal enum VersionType
    {
        Major,
        Minor,
        Patch,
        Specific,
        ReleaseCandidate
    }
    internal record Version
    {
        public Version(string v)
        {
            V = v;
        }
        public int Major => int.Parse(V.Split('.').First());
        public int Minor => int.Parse(V.Split('.').Skip(1).First());
        public int Patch => int.Parse(V.Split('.').Skip(2).First().Split('-').First());
        public int ReleaaseCandidate => V.Contains("-rc.") ? int.Parse(V.Split("-rc.").Last()) : -1;
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
                {
                    if (context.Patch > Patch)
                        return true;
                    else if (context.Patch == Patch)
                    {
                        return context.ReleaaseCandidate > ReleaaseCandidate;
                    }
                }
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
                case VersionType.ReleaseCandidate:
                    V = $"{Major}.{Minor}.{Patch}-rc.{ReleaaseCandidate + addingValue}";
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
