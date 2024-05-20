namespace Rystem.Authentication.Social.Blazor
{
    public delegate ValueTask SocialLogout(bool forceReload);
    public sealed class SocialUserWrapper<TUser>
        where TUser : SocialUser, new()
    {
        public required TUser User { get; set; }
        public required string CurrentToken { get; set; }
        public SocialLogout LogoutAsync { get; set; } = null!;
    }
}
