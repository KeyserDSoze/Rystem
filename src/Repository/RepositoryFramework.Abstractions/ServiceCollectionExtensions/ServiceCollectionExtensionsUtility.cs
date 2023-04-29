using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        private static bool s_throwExceptionIfARepositoryServiceIsAddedTwoOrMoreTimes = true;
        public static IServiceCollection ThrowExceptionIfARepositoryServiceIsAddedTwoOrMoreTimes(this IServiceCollection services)
        {
            s_throwExceptionIfARepositoryServiceIsAddedTwoOrMoreTimes = true;
            return services;
        }
        public static IServiceCollection IgnoreExceptionIfARepositoryServiceIsAddedTwoOrMoreTimes(this IServiceCollection services)
        {
            s_throwExceptionIfARepositoryServiceIsAddedTwoOrMoreTimes = false;
            return services;
        }
        public static IServiceCollection RemoveServiceIfAlreadyInstalled<TStorage>(this IServiceCollection services,
            bool checkIfItsARepository,
            params Type[] types)
        {
            foreach (var type in types)
            {
                var serviceDescriptors = services.Where(descriptor => descriptor.ServiceType == type).ToList();
                foreach (var serviceDescriptor in serviceDescriptors)
                {
                    if (checkIfItsARepository && s_throwExceptionIfARepositoryServiceIsAddedTwoOrMoreTimes)
                        throw new ArgumentException($"You have two configurations of the same interface {serviceDescriptor.ServiceType.FullName}. {typeof(TStorage).FullName} wants to override {serviceDescriptor.ImplementationType?.FullName} with lifetime {serviceDescriptor.Lifetime}.");
                    services.Remove(serviceDescriptor);
                }
            }
            return services;
        }
    }
}
