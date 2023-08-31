namespace RepositoryFramework.Api.Server.Authorization
{
    public class AuthorizedRepositoryResponse
    {
        public string? Message { get; set; }
        public bool Success { get; set; }
        public static AuthorizedRepositoryResponse Ok() => new() { Success = true };
        public static AuthorizedRepositoryResponse NotOk() => new() { Success = false };
        public static Task<AuthorizedRepositoryResponse> OkAsTask() => Task.FromResult(new AuthorizedRepositoryResponse() { Success = true });
        public static Task<AuthorizedRepositoryResponse> NotOkAsTask() => Task.FromResult(new AuthorizedRepositoryResponse() { Success = false });
        public static implicit operator bool(AuthorizedRepositoryResponse authorizedRepositoryResponse) => authorizedRepositoryResponse.Success;
        public static implicit operator AuthorizedRepositoryResponse(bool success) => new() { Success = success };
    }
}
