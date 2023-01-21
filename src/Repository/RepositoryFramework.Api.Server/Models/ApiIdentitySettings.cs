using Microsoft.Extensions.Configuration;

namespace RepositoryFramework
{
    public sealed class ApiIdentitySettings
    {
        public Uri? AuthorizationUrl { get; set; }
        public Uri? TokenUrl { get; set; }
        public string? ClientId { get; set; }
        public List<ApiIdentityScopeSettings> Scopes { get; set; } = new();
        public bool HasOpenIdAuthentication => AuthorizationUrl != null;
    }
}
