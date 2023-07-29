namespace Microsoft.Extensions.DependencyInjection
{
    public abstract class ProxyService<T>
    {
        protected T? _proxy;
        public T Proxy => _proxy!;
    }
}
