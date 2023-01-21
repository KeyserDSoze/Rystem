namespace RepositoryFramework.Test.Infrastructure.EntityFramework
{
    public record MappingUser(int Id, string Username, string Email, List<string> Groups, DateTime CreationTime);
}
