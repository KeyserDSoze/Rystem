namespace Rystem.PlayFramework.Domain.Wrappers
{
    /// <summary>
    /// Lazy service resolver that provides the service instance when accessed
    /// </summary>
    internal sealed class LazyServiceTarget
    {
        private readonly Type _serviceType;

        public LazyServiceTarget(Type serviceType)
        {
            _serviceType = serviceType;
        }

        // This will be called by reflection when AIFunctionFactory tries to get the target
        // But it won't actually be used because we override ExecuteAsync
    }
}
