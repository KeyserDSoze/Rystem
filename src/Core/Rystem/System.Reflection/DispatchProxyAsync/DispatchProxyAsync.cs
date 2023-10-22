﻿namespace System.Reflection
{
    public abstract class DispatchProxyAsync
    {
        public static T Create<T, TProxy>() where TProxy : DispatchProxyAsync
            => (T)AsyncDispatchProxyGenerator.CreateProxyInstance(typeof(TProxy), typeof(T));

        public abstract void Invoke(MethodInfo method, object[] args);
        public abstract TResponse InvokeT<TResponse>(MethodInfo method, object[] args);
        public abstract Task InvokeAsync(MethodInfo method, object[] args);
        public abstract Task<TResponse> InvokeAsyncT<TResponse>(MethodInfo method, object[] args);
        public abstract ValueTask InvokeValueAsync(MethodInfo method, object[] args);
        public abstract ValueTask<TResponse> InvokeValueAsyncT<TResponse>(MethodInfo method, object[] args);
    }
}
