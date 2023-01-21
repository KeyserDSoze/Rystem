namespace RepositoryFramework.WebApi.Models
{
    public class SuperiorUser : CreativeUser
    {
        public SuperiorUser(string email) : base(email)
        {
        }
    }
    public class SuperUser : CreativeUser
    {
        public SuperUser(string email) : base(email)
        {
        }
    }
    public class CreativeUser
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; }
        public int Port { get; set; }
        public bool IsAdmin { get; set; }
        public Guid GroupId { get; set; }
        public CreativeUser(string email)
        {
            Email = email;
        }
    }
}
