using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public sealed class ApiEndpointBuilder<T>
    {
        private readonly EndpointValue _endpointValue;
        internal ApiEndpointBuilder(EndpointValue endpointValue, bool removeAsyncSuffix)
        {
            _endpointValue = endpointValue;
            foreach (var method in typeof(T).GetMethods())
            {
                var methodName = method.Name;
                if (removeAsyncSuffix && methodName.EndsWith("Async"))
                    methodName = methodName[0..^5];
                var name = methodName;
                var counter = 1;
                while (_endpointValue.Methods.Any(x => x.Value.Name == name))
                {
                    counter++;
                    name = $"{methodName}_{counter}";
                }
                _endpointValue.Methods.Add(method.GetSignature(), new EndpointMethodValue(method, name));
            }
        }
        public ApiEndpointBuilder<T> SetEndpointName(string name)
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
        public ApiEndpointBuilder<T> SetMethodName(Expression<Func<T, Delegate>> method, string name)
        {
            var methodName = ApiEndpointBuilder<T>.GetName(method);
            var actualValue = _endpointValue.Methods.Select(x => x.Value).FirstOrDefault(x => x.Name == methodName);
            if (actualValue != null)
            {
                actualValue.Name = name;
                actualValue.Update();
            }
            return this;
        }
        public ApiEndpointBuilder<T> SetMethodName(MethodInfo methodInfo, string name)
        {
            var signature = methodInfo.GetSignature();
            if (_endpointValue.Methods.TryGetValue(signature, out var actualValue))
            {
                actualValue.Name = name;
                actualValue.Update();
            }
            return this;
        }
        public ApiEndpointBuilder<T> Remove(MethodInfo methodInfo)
        {
            var signature = methodInfo.GetSignature();
            if (_endpointValue.Methods.ContainsKey(signature))
            {
                _endpointValue.Methods.Remove(signature);
            }
            return this;
        }
        public ApiEndpointBuilder<T> Remove(string methodName)
        {
            if (_endpointValue.Methods.ContainsKey(methodName))
            {
                _endpointValue.Methods.Remove(methodName);
            }
            return this;
        }
        private ApiEndpointBuilder<T> AddAuthorization(string methodSignature, string[] policies)
        {
            if (_endpointValue.Methods.TryGetValue(methodSignature, out var actualValue))
            {
                List<string> allPolicies = new();
                if (actualValue.Policies != null)
                    allPolicies.AddRange(actualValue.Policies);
                allPolicies.AddRange(policies);
                actualValue.Policies = allPolicies.Distinct().ToArray();
                actualValue.Update();
            }
            return this;
        }
        public ApiEndpointBuilder<T> AddAuthorization(Expression<Func<T, Delegate>> method)
        {
            var methodName = ApiEndpointBuilder<T>.GetName(method);
            var actualValue = _endpointValue.Methods.FirstOrDefault(x => x.Value.Name == methodName);
            if (!actualValue.Equals(default(KeyValuePair<string, EndpointMethodValue>)))
                return AddAuthorization(actualValue.Key, Array.Empty<string>());
            else
                return this;
        }
        public ApiEndpointBuilder<T> AddAuthorization(Expression<Func<T, Delegate>> method, params string[] policies)
        {
            var methodName = ApiEndpointBuilder<T>.GetName(method);
            var actualValue = _endpointValue.Methods.FirstOrDefault(x => x.Value.Name == methodName);
            if (!actualValue.Equals(default(KeyValuePair<string, EndpointMethodValue>)))
                return AddAuthorization(actualValue.Key, policies);
            else
                return this;
        }
        public ApiEndpointBuilder<T> AddAuthorizationForAll()
        {
            foreach (var method in _endpointValue.Methods)
                AddAuthorization(method.Key, Array.Empty<string>());
            return this;
        }
        public ApiEndpointBuilder<T> AddAuthorizationForAll(params string[] policies)
        {
            foreach (var method in _endpointValue.Methods)
                AddAuthorization(method.Key, policies);
            return this;
        }
        public ApiEndpointBuilder<T> SetupParameter(Expression<Func<T, Delegate>> method, string parameterName, Action<EndpointMethodParameterValue> setup)
        {
            var methodName = ApiEndpointBuilder<T>.GetName(method);
            var actualValue = _endpointValue.Methods.Select(x => x.Value).FirstOrDefault(x => x.Name == methodName);
            if (actualValue != null)
            {
                var value = actualValue.Parameters.FirstOrDefault(x => x.Name == parameterName);
                if (value != null)
                {
                    setup.Invoke(value);
                    actualValue.Update();
                }
            }
            return this;
        }
        public ApiEndpointBuilder<T> SetupParameter(MethodInfo methodInfo, string parameterName, Action<EndpointMethodParameterValue> setup)
        {
            var signature = methodInfo.GetSignature();
            if (_endpointValue.Methods.TryGetValue(signature, out var actualValue))
            {
                var value = actualValue.Parameters.FirstOrDefault(x => x.Name == parameterName);
                if (value != null)
                {
                    setup.Invoke(value);
                    actualValue.Update();
                }
            }
            return this;
        }
    }
}
