namespace Rystem.Api
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using the request cookie.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PathAttribute : Attribute
    {
        /// <summary>
        /// If you have api/something/somethingelse/{parameter1}/{parameter2} you need to set 0 to get the first parameter or 1 if you want the second, with -1 you get all paramters in a row.
        /// </summary>
        public int Index { get; set; } = -1;
        public bool IsRequired { get; set; } = true;
    }
}
