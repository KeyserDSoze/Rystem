using Microsoft.JSInterop;

namespace Rystem.Authentication.Social.Blazor
{
    /// <summary>
    /// Platform detection utilities for Blazor (Web, MAUI iOS, MAUI Android)
    /// </summary>
    public static class PlatformDetector
    {
        /// <summary>
        /// Detect current platform based on runtime environment
        /// </summary>
        public static async Task<PlatformType> DetectPlatformAsync(IJSRuntime jsRuntime)
        {
            try
            {
                // Try to detect via JavaScript (works in Blazor WebAssembly and Server)
                var userAgent = await jsRuntime.InvokeAsync<string>("eval", "navigator.userAgent");
                
                if (userAgent.Contains("iPhone") || userAgent.Contains("iPad") || userAgent.Contains("iPod"))
                {
                    return PlatformType.iOS;
                }
                
                if (userAgent.Contains("Android"))
                {
                    return PlatformType.Android;
                }
                
                return PlatformType.Web;
            }
            catch
            {
                // Fallback: check if running in MAUI
#if IOS
                return PlatformType.iOS;
#elif ANDROID
                return PlatformType.Android;
#else
                return PlatformType.Web;
#endif
            }
        }
        
        /// <summary>
        /// Synchronous platform detection (use with caution)
        /// </summary>
        public static PlatformType DetectPlatformSync()
        {
#if IOS
            return PlatformType.iOS;
#elif ANDROID
            return PlatformType.Android;
#else
            return PlatformType.Web;
#endif
        }
        
        /// <summary>
        /// Check if running on mobile platform
        /// </summary>
        public static bool IsMobilePlatform(PlatformType platform)
        {
            return platform == PlatformType.iOS || platform == PlatformType.Android;
        }
        
        /// <summary>
        /// Check if running in Blazor Hybrid (MAUI)
        /// </summary>
        public static bool IsBlazorHybrid()
        {
#if IOS || ANDROID
            return true;
#else
            return false;
#endif
        }
        
        /// <summary>
        /// Get default redirect URI based on platform
        /// </summary>
        public static string GetDefaultRedirectUri(PlatformType platform, string? webBaseUri = null)
        {
            return platform switch
            {
                PlatformType.iOS => "msauth://com.yourapp.bundle/auth",
                PlatformType.Android => "myapp://oauth/callback",
                _ => webBaseUri ?? "http://localhost:5000"
            };
        }
    }
}
