using System;

namespace RepositoryFramework.Test.Models
{
    public class SuperUser
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; }
        public int Port { get; set; }
        public bool IsAdmin { get; set; }
        public Guid GroupId { get; set; }
        public SuperUser(string email)
        {
            Email = email;
        }
    }
}
