namespace RepositoryFramework.Api.Server.Authorization
{
    public class AuthorizedRepositoryResponse
    {
        public string? Message { get; set; }
        public bool Success { get; set; }
        public static AuthorizedRepositoryResponse Ok() => new() { Success = true };
        public static AuthorizedRepositoryResponse NotOk() => new() { Success = false };
    }
}
