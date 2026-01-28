namespace Rystem.Authentication.Social.Blazor
{
    /// <summary>
    /// Platform-specific configuration for social authentication
    /// </summary>
    public sealed class PlatformConfig
    {
        /// <summary>
        /// Platform type (Web, iOS, Android, Auto)
        /// Default: Auto (auto-detect based on runtime)
        /// </summary>
        public PlatformType Type { get; set; } = PlatformType.Auto;
        
        /// <summary>
        /// Redirect path or complete URI for OAuth callback
        /// 
        /// Smart detection:
        /// - If contains "://" → treated as complete URI (e.g., "myapp://oauth/callback" for mobile)
        /// - If starts with "/" → treated as path, prepended with NavigationManager.BaseUri (e.g., "/account/login" for web)
        /// - Default: "/account/login"
        /// 
        /// Examples:
        /// - Web: "/account/login" → becomes "https://yourdomain.com/account/login"
        /// - iOS: "msauth://com.yourapp.bundle/auth" → used as-is
        /// - Android: "myapp://oauth/callback" → used as-is
        /// - Custom web: "https://custom.domain.com/callback" → used as-is
        /// </summary>
        public string? RedirectPath { get; set; }
        
        /// <summary>
        /// Login mode (Redirect or Popup)
        /// Default: Redirect (Popup not yet supported in Blazor)
        /// </summary>
        public LoginMode LoginMode { get; set; } = LoginMode.Redirect;
    }
}
