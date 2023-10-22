namespace System.Reflection
{
    public class DispatchProxyHandler
    {
        public DispatchProxyHandler()
        {
        }
        public void InvokeHandle(object[] args) => AsyncDispatchProxyGenerator.Invoke(args);
        public T InvokeHandleT<T>(object[] args) => AsyncDispatchProxyGenerator.Invoke<T>(args);
        public Task InvokeAsyncHandle(object[] args) => AsyncDispatchProxyGenerator.InvokeAsync(args);
        public Task<T> InvokeAsyncHandleT<T>(object[] args) => AsyncDispatchProxyGenerator.InvokeAsync<T>(args);
        public ValueTask InvokeValueAsyncHandle(object[] args) => AsyncDispatchProxyGenerator.InvokeValueAsync(args);
        public ValueTask<T> InvokeValueAsyncHandleT<T>(object[] args) => AsyncDispatchProxyGenerator.InvokeValueAsync<T>(args);
    }
}
