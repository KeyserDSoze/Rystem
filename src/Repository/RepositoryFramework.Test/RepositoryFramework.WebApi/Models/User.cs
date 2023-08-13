using System.ComponentModel;

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
    public class NonPlusSuperUserKey : IKey
    {
        public string A { get; set; }
        public string B { get; set; }
        public static IKey Parse(string keyAsString)
        {
            var splitted = keyAsString.Split("^^^^");
            return new NonPlusSuperUserKey
            {
                A = splitted[0],
                B = splitted[1]
            };
        }

        public string AsString()
        {
            return $"{A}^^^^{B}";
        }
    }
    public class PlusSuperUserKey
    {
        public string A { get; set; }
        public string B { get; set; }
    }
    public class NonPlusSuperUser : CreativeUser
    {
        public NonPlusSuperUser(string email) : base(email)
        {
        }
    }
    public class CreativeUser
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; }
        [Description("Port is great")]
        public int Port { get; set; }
        public bool IsAdmin { get; set; }
        public Guid GroupId { get; set; }
        public CreativeUser(string email)
        {
            Email = email;
        }
    }
}
