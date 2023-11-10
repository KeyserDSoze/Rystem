namespace Rystem.Authentication.Social
{
    public class SocialLoginSettings: SocialDefaultLoginSettings
    {
        public string? ClientId { get; set; }
        public override bool IsActive => ClientId != null;
    }
}
