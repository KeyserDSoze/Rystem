namespace Rystem.Authentication.Social.Blazor
{
    /// <summary>
    /// Platform types for social authentication
    /// </summary>
    public enum PlatformType
    {
        /// <summary>
        /// Web browser platform (default)
        /// </summary>
        Web = 0,
        
        /// <summary>
        /// iOS mobile platform (MAUI, Blazor Hybrid)
        /// </summary>
        iOS = 1,
        
        /// <summary>
        /// Android mobile platform (MAUI, Blazor Hybrid)
        /// </summary>
        Android = 2,
        
        /// <summary>
        /// Auto-detect platform based on runtime environment
        /// </summary>
        Auto = 3
    }
}
