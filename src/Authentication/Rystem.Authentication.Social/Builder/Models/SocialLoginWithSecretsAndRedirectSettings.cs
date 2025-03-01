namespace Rystem.Authentication.Social
{
    public class SocialLoginWithSecretsAndRedirectSettings : SocialLoginWithSecretsSettings
    {
        private const string HttpScheme = "http";
        public List<string>? AllowedDomains { get; set; }
        public override bool IsActive => ClientId != null && ClientSecret != null && AllowedDomains != null && AllowedDomains.Count > 0;
        public string? CheckDomain(string? domain)
        {
            if (domain == null)
            {
                return AllowedDomains?.FirstOrDefault();
            }
            else
            {
                var domainFromUri = domain.StartsWith(HttpScheme) ? new Uri(domain).Host : domain;
                if (AllowedDomains?.Contains(domainFromUri) == true)
                    return domain;
            }
            return default;
        }
    }
}
