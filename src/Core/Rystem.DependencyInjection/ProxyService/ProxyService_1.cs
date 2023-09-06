namespace Microsoft.Extensions.DependencyInjection
{
    public abstract class ProxyService<T>
    {
        protected T? _proxy;
        protected object[]? _parameters;
        protected Type? _proxyType;
        public T Proxy
        {
            get
            {
                try
                {
                    _proxy ??= (T)Activator.CreateInstance(_proxyType, _parameters);
                    return _proxy;
                }
                catch (Exception ex)
                {
                    var counter = 0;
                    throw new ArgumentException($"Error with instance creation for {_proxyType?.FullName} with parameters length {_parameters?.Length} and types {(_parameters != null ? string.Join(" | ", _parameters.Select(x => x.GetType()?.FullName ?? string.Empty)) : string.Empty)}. Public available constructors {(_proxyType != null ? _proxyType?.GetConstructors().Length : string.Empty)} with parameters {(_proxyType != null ? string.Join($"; {++counter}) ", _proxyType?.GetConstructors().Select(x => string.Join(" | ", x.GetParameters().Select(x => x.ParameterType.FullName)))) : string.Empty)}", ex);
                }
            }
        }
    }
}
