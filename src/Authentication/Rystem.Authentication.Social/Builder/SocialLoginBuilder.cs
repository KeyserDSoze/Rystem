namespace Rystem.Authentication.Social
{
    public sealed class SocialLoginBuilder
    {
        public SocialLoginSettings Google { get; set; } = new();
        public SocialLoginSettings Microsoft { get; set; } = new();
        public SocialLoginSettings Facebook { get; set; } = new();
    }
}
