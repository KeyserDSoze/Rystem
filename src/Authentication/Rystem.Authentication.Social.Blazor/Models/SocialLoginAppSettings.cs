namespace Rystem.Authentication.Social.Blazor
{
    public sealed class SocialLoginAppSettings
    {
        public string? ApiUrl { get; set; }
        public SocialParameter Google { get; set; } = new();
        public SocialParameter Facebook { get; set; } = new();
        public SocialParameter Microsoft { get; set; } = new();
        
        /// <summary>
        /// Platform-specific configuration (Web, iOS, Android)
        /// Default: Auto-detect platform
        /// </summary>
        public PlatformConfig Platform { get; set; } = new();
        
        /// <summary>
        /// Default login mode (Redirect or Popup)
        /// Note: Popup mode not yet implemented in Blazor
        /// Default: Redirect
        /// </summary>
        public LoginMode LoginMode { get; set; } = LoginMode.Redirect;
    }
}
