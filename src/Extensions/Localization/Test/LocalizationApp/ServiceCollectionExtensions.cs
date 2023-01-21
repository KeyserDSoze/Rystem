namespace LocalizationApp
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInAppLocalization(this IServiceCollection services)
        {
            services.AddMultipleLocalization<Shared2>(x =>
            {
                x.ResourcesPath = "Resources";
            });
            //services.AddLocalization(x =>
            //{
            //    x.ResourcesPath = "Resources";
            //});
            return services;
        }
    }
}
