namespace Microsoft.Extensions.DependencyInjection
{
    public abstract class ProxyService<T>
    {
        protected T? _proxy;
        protected object[]? _parameters;
        protected Type? _proxyType;
        public T Proxy => _proxy ?? (T)Activator.CreateInstance(_proxyType, _parameters);
    }
}
