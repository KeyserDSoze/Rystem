namespace Rystem.Authentication.Social
{
    public interface ILocalizedSocialUser : ISocialUser
    {
        string? Language { get; set; }
    }
}
