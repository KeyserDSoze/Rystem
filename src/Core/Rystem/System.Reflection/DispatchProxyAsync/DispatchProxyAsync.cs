namespace System.Reflection
{
    public abstract class DispatchProxyAsync
    {
        public static T Create<T, TProxy>() where TProxy : DispatchProxyAsync 
            => (T)AsyncDispatchProxyGenerator.CreateProxyInstance(typeof(TProxy), typeof(T));

        public abstract object Invoke(MethodInfo method, object[] args);

        public abstract Task InvokeAsync(MethodInfo method, object[] args);

        public abstract Task<TResponse> InvokeAsyncT<TResponse>(MethodInfo method, object[] args);
    }
}
