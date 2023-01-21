namespace RepositoryFramework.WebApi.Models
{
    public class Car
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }
    public class Car2 : Car { }
}
