using System.Text.Json.Serialization;
using RepositoryFramework;
using RepositoryFramework.Api.Client;

namespace Rystem.Authentication.Social.TestApi.Models
{
    public sealed class SocialRole
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Other { get; set; }
    }
    public sealed class SocialRoleA : IRepositoryClientInterceptor
    {
        public SocialRoleA()
        {

        }
        public Task<HttpClient> EnrichAsync(HttpClient client, RepositoryMethods path)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class SocialUser : ISocialUser
    {
        [JsonPropertyName("u")]
        public string? Username { get; set; }
    }
}
