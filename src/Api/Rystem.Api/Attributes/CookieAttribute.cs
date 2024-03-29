﻿namespace Rystem.Api
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using the request cookie.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CookieAttribute : Attribute
    {
        public string? Name { get; set; }
        public bool IsRequired { get; set; } = true;
    }
}
