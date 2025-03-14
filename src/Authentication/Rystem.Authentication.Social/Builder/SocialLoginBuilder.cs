﻿using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Authentication.Social
{
    public sealed class SocialLoginBuilder
    {
        public SocialLoginWithSecretsAndRedirectSettings Google { get; set; } = new();
        public SocialLoginWithSecretsAndRedirectSettings Microsoft { get; set; } = new();
        public SocialDefaultLoginSettings Facebook { get; set; } = new();
        public SocialDefaultLoginSettings Amazon { get; set; } = new();
        public SocialLoginWithSecretsSettings GitHub { get; set; } = new();
        public SocialLoginWithSecretsAndRedirectSettings Linkedin { get; set; } = new();
        public SocialLoginWithSecretsAndRedirectSettings X { get; set; } = new();
        public SocialLoginWithSecretsAndRedirectSettings Instagram { get; set; } = new();
        public SocialLoginWithSecretsAndRedirectSettings Pinterest { get; set; } = new();
        public SocialLoginWithSecretsAndRedirectSettings TikTok { get; set; } = new();
    }
}
