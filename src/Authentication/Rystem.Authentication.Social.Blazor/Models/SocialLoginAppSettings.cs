namespace Rystem.Authentication.Social.Blazor
{
    public sealed class SocialLoginAppSettings
    {
        public string? ApiUrl { get; set; }
        public SocialParameter Google { get; set; } = new();
        public SocialParameter Facebook { get; set; } = new();
        public SocialParameter Microsoft { get; set; } = new();
    }
}
