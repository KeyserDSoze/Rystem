using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class HeaderAttribute : Attribute, IModelNameProvider, IFromHeaderMetadata
    {
        /// <inheritdoc />
        public string? Name { get; set; }
        public bool IsRequired { get; set; } = true;
    }
}
