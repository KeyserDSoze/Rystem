namespace System.Reflection
{
    public class MethodInfoWrapper
    {
        private readonly MethodInfo _method;
        public MethodInfoWrapper(MethodInfo methodInfo)
            => _method = methodInfo;
        public dynamic Invoke(object obj, params object[] inputs)
          => _method.Invoke(obj, inputs)!;
        public TResult? Invoke<TResult>(object obj, params object[] inputs)
        {
            var value = _method.Invoke(obj, inputs);
            return (TResult?)value;
        }
        public async ValueTask<dynamic> InvokeAsValueTaskAsync(object obj, params object[] inputs)
          => await (dynamic)_method.Invoke(obj, inputs)!;
        public async Task<dynamic> InvokeAsync(object obj, params object[] inputs)
          => await (dynamic)_method.Invoke(obj, inputs)!;
        public async ValueTask<T> InvokeAsValueTaskAsync<T>(object obj, params object[] inputs)
          => (T)(await (dynamic)_method.Invoke(obj, inputs)!);
        public async Task<T> InvokeAsync<T>(object obj, params object[] inputs)
          => (T)(await (dynamic)_method.Invoke(obj, inputs)!);
    }
}
