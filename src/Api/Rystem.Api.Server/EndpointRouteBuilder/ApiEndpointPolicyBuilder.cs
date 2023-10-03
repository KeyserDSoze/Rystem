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
        public void SetEndpointName(string name)
        {
            _endpointValue.EndpointName = name;
        }
        private static string GetName(Expression<Func<T, Delegate>> expression)
        {
            var unaryExpression = (UnaryExpression)expression.Body;
            var methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
            var methodInfoExpression = (ConstantExpression)methodCallExpression.Object!;
            var methodInfo = (MemberInfo)methodInfoExpression.Value!;
            return methodInfo!.Name;
        }
        public void SetMethodName(Expression<Func<T, Delegate>> method, string name)
        {
            var methodName = ApiEndpointPolicyBuilder<T>.GetName(method);
            if (_endpointValue.Methods.TryGetValue(methodName, out var actualValue))
            {
                actualValue.Name = methodName;
            }
        }

        private void AddAuthorization(string methodName, string[] policies)
        {
            if (_endpointValue.Methods.TryGetValue(methodName, out var actualValue))
            {
                List<string> allPolicies = new();
                if (actualValue.Policies != null)
                    allPolicies.AddRange(actualValue.Policies);
                allPolicies.AddRange(policies);
                actualValue.Policies = allPolicies.Distinct().ToArray();
            }
        }
        public void AddAuthorization(Expression<Func<T, Delegate>> method)
        {
            AddAuthorization(ApiEndpointPolicyBuilder<T>.GetName(method), Array.Empty<string>());
        }
        public void AddAuthorization(Expression<Func<T, Delegate>> method, params string[] policies)
        {
            AddAuthorization(ApiEndpointPolicyBuilder<T>.GetName(method), policies);
        }
        public void AddAuthorizationForAll()
        {
            foreach (var method in typeof(T).GetMethods())
                AddAuthorization(method.Name, Array.Empty<string>());
        }
        public void AddAuthorizationForAll(params string[] policies)
        {
            foreach (var method in typeof(T).GetMethods())
                AddAuthorization(method.Name, policies);
        }
    }
}
