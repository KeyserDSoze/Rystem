using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using the request query string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class QueryAttribute : Attribute, IModelNameProvider, IFromQueryMetadata
    {
        /// <inheritdoc />
        public string? Name { get; set; }
        public bool IsRequired { get; set; } = true;
    }
}
