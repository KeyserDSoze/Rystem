namespace RepositoryFramework.Test.Domain
{
    public record AppUser(int Id, string Username, string Email, List<string> Groups, DateTime CreationTime);
}
