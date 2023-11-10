namespace Rystem.Authentication.Social
{
    public class SocialLoginWithSecretsAndRedirectSettings : SocialLoginWithSecretsSettings
    {
        public string? ClientSecret { get; set; }
        public string? RedirectDomain { get; set; }
        public override bool IsActive => ClientId != null && ClientSecret != null && RedirectDomain != null;
    }
}
