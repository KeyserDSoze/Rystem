using System.Collections.Generic;
using System.Security.Cryptography;

namespace Rystem.Test.UnitTest.System.Population.Random.Models
{
    public sealed class AppUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public List<Group> Groups { get; set; }
        public AppSettings Settings { get; init; }
        public InternalAppSettings InternalAppSettings { get; set; }
        public List<string> Claims { get; set; }
        public string MainGroup { get; set; }
        public string? HashedMainGroup => MainGroup?.ToHash();
    }
}
