namespace Rystem.Authentication.Social
{
    public sealed class SocialLoginBuilder
    {
        public SocialLoginWithSecretsAndRedirectSettings Google { get; set; } = new();
        public SocialDefaultLoginSettings Microsoft { get; set; } = new();
        public SocialDefaultLoginSettings Facebook { get; set; } = new();
        public SocialDefaultLoginSettings Amazon { get; set; } = new();
        public SocialLoginWithSecretsSettings GitHub { get; set; } = new();
        public SocialLoginWithSecretsAndRedirectSettings Linkedin { get; set; } = new();
    }
}
