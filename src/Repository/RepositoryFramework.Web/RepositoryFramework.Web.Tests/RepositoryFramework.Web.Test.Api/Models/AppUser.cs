using System.Security.Cryptography;

namespace RepositoryFramework.Web.Test.BlazorApp.Models
{
    public sealed class AppUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public List<Group> Groups { get; set; } = null!;
        public AppSettings Settings { get; init; } = null!;
        public InternalAppSettings InternalAppSettings { get; set; } = null!;
        public List<string> Claims { get; set; } = null!;
        public string MainGroup { get; set; } = null!;
        public string? HashedMainGroup => MainGroup?.ToHash();
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
    }
    public sealed class InternalAppSettings
    {
        public int Index { get; set; }
        public string Options { get; set; } = null!;
        public List<string> Maps { get; set; } = null!;
    }
}
