namespace Rystem.Authentication.Social
{
    /// <summary>
    /// Settings for token validation and exchange with OAuth providers
    /// </summary>
    public sealed class TokenCheckerSettings
    {
        /// <summary>
        /// Domain for OAuth redirect (e.g., https://yourdomain.com)
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// Redirect path after OAuth callback (default: /)
        /// </summary>
        public string? RedirectPath { get; set; }

        /// <summary>
        /// Additional parameters for token exchange (e.g., code_verifier for PKCE)
        /// </summary>
        public Dictionary<string, string>? AdditionalParameters { get; set; }

        /// <summary>
        /// Get full redirect URI
        /// </summary>
        public string GetRedirectUri()
        {
            if (string.IsNullOrWhiteSpace(Domain))
                return RedirectPath ?? string.Empty;

            return $"{Domain.TrimEnd('/')}{RedirectPath}";
        }

        /// <summary>
        /// Get additional parameter by key
        /// </summary>
        public string? GetParameter(string key)
        {
            if (AdditionalParameters == null || string.IsNullOrWhiteSpace(key))
                return null;

            return AdditionalParameters.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Set additional parameter
        /// </summary>
        public TokenCheckerSettings WithParameter(string key, string value)
        {
            AdditionalParameters ??= new Dictionary<string, string>();
            AdditionalParameters[key] = value;
            return this;
        }
    }
}
