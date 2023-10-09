using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public sealed class ApiEndpointPolicyBuilder<T>
    {
        private readonly EndpointValue _endpointValue;
        internal ApiEndpointPolicyBuilder(EndpointValue endpointValue)
        {
            _endpointValue = endpointValue;
            foreach (var method in typeof(T).GetMethods())
            {
                _endpointValue.Methods.Add(method.Name, new EndpointMethodValue(method));
            }
        }
        public ApiEndpointPolicyBuilder<T> SetEndpointName(string name)
        {
            _endpointValue.EndpointName = name;
            return this;
        }
        private static string GetName(Expression<Func<T, Delegate>> expression)
        {
            var unaryExpression = (UnaryExpression)expression.Body;
            var methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
            var methodInfoExpression = (ConstantExpression)methodCallExpression.Object!;
            var methodInfo = (MemberInfo)methodInfoExpression.Value!;
            return methodInfo!.Name;
        }
        public ApiEndpointPolicyBuilder<T> SetMethodName(Expression<Func<T, Delegate>> method, string name)
        {
            var methodName = ApiEndpointPolicyBuilder<T>.GetName(method);
            if (_endpointValue.Methods.TryGetValue(methodName, out var actualValue))
            {
                actualValue.Name = name;
            }
            return this;
        }
        private ApiEndpointPolicyBuilder<T> AddAuthorization(string methodName, string[] policies)
        {
            if (_endpointValue.Methods.TryGetValue(methodName, out var actualValue))
            {
                List<string> allPolicies = new();
                if (actualValue.Policies != null)
                    allPolicies.AddRange(actualValue.Policies);
                allPolicies.AddRange(policies);
                actualValue.Policies = allPolicies.Distinct().ToArray();
            }
            return this;
        }
        public ApiEndpointPolicyBuilder<T> AddAuthorization(Expression<Func<T, Delegate>> method)
        {
            return AddAuthorization(ApiEndpointPolicyBuilder<T>.GetName(method), Array.Empty<string>());
        }
        public ApiEndpointPolicyBuilder<T> AddAuthorization(Expression<Func<T, Delegate>> method, params string[] policies)
        {
            return AddAuthorization(ApiEndpointPolicyBuilder<T>.GetName(method), policies);
        }
        public ApiEndpointPolicyBuilder<T> AddAuthorizationForAll()
        {
            foreach (var method in typeof(T).GetMethods())
                AddAuthorization(method.Name, Array.Empty<string>());
            return this;
        }
        public ApiEndpointPolicyBuilder<T> AddAuthorizationForAll(params string[] policies)
        {
            foreach (var method in typeof(T).GetMethods())
                AddAuthorization(method.Name, policies);
            return this;
        }
        public ApiEndpointPolicyBuilder<T> SetupParameter(Expression<Func<T, Delegate>> method, string parameterName, Action<EndpointMethodParameterValue> setup)
        {
            var methodName = ApiEndpointPolicyBuilder<T>.GetName(method);
            if (_endpointValue.Methods.TryGetValue(methodName, out var actualValue))
            {
                var value = actualValue.Parameters.FirstOrDefault(x => x.Name == parameterName);
                if (value != null)
                {
                    setup.Invoke(value);
                }
            }
            return this;
        }
    }
}
