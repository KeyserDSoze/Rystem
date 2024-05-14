namespace Rystem.Authentication.Social.Blazor
{
    public sealed class State
    {
        public required SocialLoginProvider Provider { get; set; }
        public required string Value { get; set; }
        public required DateTime ExpiringTime { get; set; }
        public required string Path { get; set; }
    }
}
