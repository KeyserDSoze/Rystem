using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class SocialLoginAppSettings
    {
        public SocialParameter Google { get; set; } = new();
        public SocialParameter Facebook { get; set; } = new();
        public SocialParameter Microsoft { get; set; } = new();
    }
    public sealed class SocialParameter
    {
        public string? ClientId { get; set; }
    }
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSocialLoginUI(this IServiceCollection services,
            Action<SocialLoginAppSettings> settings)
        {
            var options = new SocialLoginAppSettings()
            {
            };
            settings.Invoke(options);
            services.AddSingleton(options);
            return services;
        }
    }
}
