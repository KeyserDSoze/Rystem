namespace Rystem.PlayFramework
{
    public sealed class ChatBuilderSettings
    {
        public string? Uri { get; set; }
        public string? ApiKey { get; set; }
        public string? Model { get; set; }
        public string? ManagedIdentityId { get; set; }
        public bool UseSystemManagedIdentity { get; set; }
        public Action<IChatClientBuilder>? ChatClientBuilder { get; set; }
    }
}
