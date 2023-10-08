using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using form-data in the request body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FormAttribute : Attribute, IModelNameProvider, IFromFormMetadata
    {
        /// <inheritdoc />
        public string? Name { get; set; }
        public bool IsRequired { get; set; } = true;
    }
}
