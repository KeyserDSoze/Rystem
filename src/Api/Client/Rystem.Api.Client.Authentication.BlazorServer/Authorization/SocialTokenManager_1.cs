using Microsoft.AspNetCore.Components;
using Rystem.Authentication.Social.Blazor;

namespace Rystem.Api.Client
{
    internal sealed class SocialTokenManager<T> : SocialTokenManager
    {
        public SocialTokenManager(SocialLoginManager socialLoginManager,
            NavigationManager? navigationManager) : base(socialLoginManager, navigationManager)
        {
        }
    }
}
