namespace System.Reflection
{
    public class DispatchProxyHandler
    {
        public DispatchProxyHandler()
        {
        }
        public void InvokeHandle(object[] args) => AsyncDispatchProxyGenerator.Invoke(args);
        public T InvokeHandle<T>(object[] args) => AsyncDispatchProxyGenerator.Invoke<T>(args);
        public Task InvokeAsyncHandle(object[] args) => AsyncDispatchProxyGenerator.InvokeAsync(args);
        public Task<T> InvokeAsyncHandleT<T>(object[] args) => AsyncDispatchProxyGenerator.InvokeAsync<T>(args);
    }
}
