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
        public async ValueTask<dynamic> InvokeAsync(object obj, params object[] inputs)
          => await (dynamic)_method.Invoke(obj, inputs)!;
    }
}
