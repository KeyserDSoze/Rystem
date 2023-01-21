using System.Reflection;

namespace RepositoryFramework
{
    internal sealed class ApiSettings
    {
        public static ApiSettings Instance { get; } = new ApiSettings();
        private ApiSettings() { }
        public string DescriptiveName { get; set; } = Assembly.GetExecutingAssembly().GetName().Name!;
        public Dictionary<string, string> Names { get; } = new();
        public string Path { get; set; } = "api";
        public string? Version { get; set; }
        public bool HasDefaultCors { get; set; }
        public bool CorsInstalled { get; set; }
        public string StartingPath => $"{Path}{(string.IsNullOrWhiteSpace(Version) ? string.Empty : $"/{Version}")}";
        public bool HasDocumentation { get; set; }
        public bool HasSwagger { get; set; }
        public bool SwaggerInstalled { get; set; }
        public ApiIdentitySettings OpenIdIdentity { get; set; } = new();
        public bool HasOpenIdAuthentication => OpenIdIdentity.HasOpenIdAuthentication;
        internal const string AllowSpecificOrigins = nameof(AllowSpecificOrigins);
    }
}
