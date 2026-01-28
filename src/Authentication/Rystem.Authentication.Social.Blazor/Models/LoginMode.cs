namespace Rystem.Authentication.Social.Blazor
{
    /// <summary>
    /// Login interaction mode for OAuth authentication
    /// </summary>
    public enum LoginMode
    {
        /// <summary>
        /// Navigate to OAuth provider in same window (default for mobile)
        /// </summary>
        Redirect = 0,
        
        /// <summary>
        /// Open OAuth provider in new window/tab (default for web - Not yet implemented in Blazor)
        /// </summary>
        Popup = 1
    }
}
