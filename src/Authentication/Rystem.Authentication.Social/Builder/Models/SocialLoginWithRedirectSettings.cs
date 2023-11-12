namespace Rystem.Authentication.Social
{
    public class SocialLoginWithRedirectSettings : SocialLoginSettings
    {
        public string? RedirectDomain { get; set; }
        public override bool IsActive => ClientId != null && RedirectDomain != null;
    }
}
