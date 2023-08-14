using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static int ScanWithReferences<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
            => services.Scan(typeof(T), lifetime, AddReferencedAssemblies(assemblies));
        public static int ScanWithReferences(
            this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
            => services.Scan(serviceType, lifetime, AddReferencedAssemblies(assemblies));
        public static int ScanWithReferences(
           this IServiceCollection services,
           ServiceLifetime lifetime,
            params Assembly[] assemblies)
            => services.Scan(lifetime, AddReferencedAssemblies(assemblies));
        private static Assembly[] AddReferencedAssemblies(params Assembly[] assemblies)
        {
            var referenced = assemblies.ToList();
            foreach (var assembly in assemblies)
            {
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    var actualAssembly = Assembly.Load(reference);
                    if (!referenced.Contains(actualAssembly))
                        referenced.Add(actualAssembly);
                }
            }
            return referenced.ToArray();
        }
    }
}
