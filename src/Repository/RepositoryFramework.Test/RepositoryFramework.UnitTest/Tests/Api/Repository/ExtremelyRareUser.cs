using System;

namespace RepositoryFramework.Test.Repository
{
    public class ExtremelyRareUser
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public int Port { get; set; }
        public bool IsAdmin { get; set; }
        public Guid GroupId { get; set; }
    }
}
