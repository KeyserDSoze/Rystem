using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    public interface ISocialUser
    {
        string? Username { get; set; }
        public static ISocialUser Empty { get; } = new DefaultSocialUser();
        public static ISocialUser OnlyUsername(string? username) => new DefaultSocialUser { Username = username };
    }
}
