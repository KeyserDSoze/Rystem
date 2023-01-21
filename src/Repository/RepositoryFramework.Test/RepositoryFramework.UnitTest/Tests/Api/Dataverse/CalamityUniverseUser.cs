using System;

namespace RepositoryFramework.Test.Models
{
    public class CalamityUniverseUser
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Port { get; set; }
        public bool IsAdmin { get; set; }
        public Guid GroupId { get; set; }
    }
}
