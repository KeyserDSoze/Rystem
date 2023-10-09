namespace Rystem.Api
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using form-data in the request body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FormAttribute : Attribute
    {
        /// <inheritdoc />
        public string? Name { get; set; }
        public bool IsRequired { get; set; } = true;
    }
}
