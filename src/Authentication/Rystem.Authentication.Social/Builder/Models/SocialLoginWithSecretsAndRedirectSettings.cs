namespace Rystem.Authentication.Social
{
    public class SocialLoginWithSecretsAndRedirectSettings : SocialLoginWithSecretsSettings
    {
        private const string HttpScheme = "http";
        private List<Uri>? _allowedDomains;
        public SocialLoginWithSecretsAndRedirectSettings AddDomainWithProtocolAndPort(string domain, string protocol, int port)
            => AddDomainWithProtocolAndPort(new Uri($"{protocol}://{domain}:{port}"));
        public SocialLoginWithSecretsAndRedirectSettings AddDomainWithProtocolAndPort(Uri uri)
        {
            if (_allowedDomains == null)
                _allowedDomains = [];
            _allowedDomains.Add(uri);
            return this;
        }
        public SocialLoginWithSecretsAndRedirectSettings AddUri(string uri)
        {
            if (!uri.Contains("http"))
                uri = $"https://{uri}";
            return AddDomainWithProtocolAndPort(new Uri(uri));
        }
        public SocialLoginWithSecretsAndRedirectSettings AddUris(params string[] uris)
        {
            foreach (var uri in uris)
                AddUri(uri);
            return this;
        }
        public SocialLoginWithSecretsAndRedirectSettings AddDomainsWithProtocolAndPort(params Uri[] uris)
        {
            foreach (var uri in uris)
                AddDomainWithProtocolAndPort(uri);
            return this;
        }
        public override bool IsActive => ClientId != null && ClientSecret != null && _allowedDomains != null && _allowedDomains.Count > 0;
        public string? CheckDomain(string? domain)
        {
            if (domain == null)
            {
                return _allowedDomains?.FirstOrDefault()?.AbsoluteUri;
            }
            else
            {
                var domainFromUri = domain.StartsWith(HttpScheme) ? new Uri(domain) : new Uri($"https://{domain}");
                var currentDomain = _allowedDomains?.FirstOrDefault(t => t.Port == domainFromUri.Port && t.Scheme == domainFromUri.Scheme && t.Host == domainFromUri.Host);
                if (currentDomain != null)
                    return currentDomain.AbsoluteUri;
            }
            return default;
        }
    }
}
