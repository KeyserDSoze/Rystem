namespace Rystem.Authentication.Social
{
    public class SocialLoginWithSecretsSettings : SocialLoginSettings
    {
        public string? ClientSecret { get; set; }
        public override bool IsActive => ClientId != null && ClientSecret != null;
    }
}
