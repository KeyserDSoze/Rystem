using System.Security.Cryptography;
using System.Text;

namespace Rystem.Authentication.Social.Blazor
{
    /// <summary>
    /// Generate PKCE (Proof Key for Code Exchange) values for OAuth 2.0
    /// </summary>
    public static class PkceGenerator
    {
        /// <summary>
        /// Generate a random code_verifier
        /// </summary>
        public static string GenerateCodeVerifier()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            
            return Base64UrlEncode(bytes);
        }

        /// <summary>
        /// Generate code_challenge from code_verifier using SHA256
        /// </summary>
        public static string GenerateCodeChallenge(string codeVerifier)
        {
            var bytes = Encoding.UTF8.GetBytes(codeVerifier);
            var hash = SHA256.HashData(bytes);
            
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
