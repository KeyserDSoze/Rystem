using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace RepositoryFramework.Api.Server.Authorization
{
    public sealed class RepositoryRequirement : IAuthorizationRequirement
    {
        public Type Type { get; }
        public Func<IHttpContextAccessor, Task<RepositoryRequirementReader>> EntityReader { get; }
        public MethodInfo Handler { get; }
        public string PolicyName { get; }
        public RepositoryRequirement(string policyName, Type type, Type keyType, Type valueType, Type handler, Func<IHttpContextAccessor, Task<RepositoryRequirementReader>> entityReader)
        {
            Type = type;
            EntityReader = entityReader;
            PolicyName = policyName;
            Handler = handler.GetMethod(nameof(IRepositoryAuthorization<string, string>.HandleRequirementAsync))!;
        }
    }
}
