namespace System.Reflection
{
    public class DispatchProxyHandler
    {
        public DispatchProxyHandler()
        {
        }
        public object InvokeHandle(object[] args) => AsyncDispatchProxyGenerator.Invoke(args);

        public Task InvokeAsyncHandle(object[] args) => AsyncDispatchProxyGenerator.InvokeAsync(args);

        public Task<T> InvokeAsyncHandleT<T>(object[] args) => AsyncDispatchProxyGenerator.InvokeAsync<T>(args);
    }
}
