using System.Security.Cryptography;

namespace RepositoryFramework.Web.Test.BlazorApp.Models
{
    public sealed class AppLanguagedDescriptionUser
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public sealed class AppUser2 : AppUser
    {
    }
    public class AppUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public Dictionary<string, AppLanguagedDescriptionUser> Descriptions { get; set; }
        public List<Group> Groups { get; set; } = null!;
        public List<MegaGroup> MegaGroups { get; set; } = null!;
        public AppSettings Settings { get; init; } = null!;
        public InternalAppSettings InternalAppSettings { get; set; } = null!;
        public List<string> Claims { get; set; } = null!;
        public string MainGroup { get; set; } = null!;
        public SuperFlag Flag { get; set; }
        public string? HashedMainGroup => MainGroup?.ToHash();
    }
    [Flags]
    public enum SuperFlag
    {
        None = 0,
        A = 1,
        B = 2,
        C = 4,
        D = 8
    }
    public sealed class MegaGroup
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public List<MegaIperGroup> Groups { get; set; } = null!;
    }
    public sealed class MegaIperGroup
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
    public sealed class Group
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
    public sealed class AppSettings
    {
        public string Color { get; set; } = null!;
        public string Options { get; set; } = null!;
        public List<string> Maps { get; set; } = null!;
        public SuperFlag Flag { get; set; }
    }
    public sealed class InternalAppSettings
    {
        public int Index { get; set; }
        public string Options { get; set; } = null!;
        public List<string> Maps { get; set; } = null!;
    }
}
