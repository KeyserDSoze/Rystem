using System.ComponentModel;

namespace Rystem.PlayFramework.Api.Services;

/// <summary>
/// Service for geometric shape operations.
/// Intentionally uses AnyOf parameters to reproduce PlayFramework tool-dispatch errors
/// when parameters are discriminated unions (AnyOf).
/// </summary>
public interface IShapeService
{
    /// <summary>
    /// Describes a shape by its name (string) or numeric code (int).
    /// Parameter is AnyOf&lt;string, int&gt; to test discriminated union deserialization.
    /// </summary>
    string DescribeShape(AnyOf<string, int> shapeNameOrCode);

    /// <summary>
    /// Calculates the area of a shape.
    /// The shape can be specified as a Circle or a Rectangle.
    /// Parameter is AnyOf&lt;CircleArgs, RectangleArgs&gt; to test complex object discriminated union.
    /// </summary>
    double CalculateArea(AnyOf<CircleArgs, RectangleArgs> shape);

    /// <summary>
    /// Returns the perimeter of a shape, accepting either a name (string) or
    /// a pre-computed value (double) as override.
    /// </summary>
    string GetShapeInfo(
        [Description("The shape name (e.g. 'circle', 'rectangle', 'triangle') or its numeric code (1=circle, 2=rectangle, 3=triangle)")] AnyOf<string, int> shapeIdentifier,
        [Description("Optional override value for the area. Provide a number to override or a string label.")] AnyOf<double, string>? areaOverride = null);
}

/// <summary>Circle shape arguments.</summary>
public sealed class CircleArgs
{
    [Description("Radius of the circle")]
    public double Radius { get; set; }
}

/// <summary>Rectangle shape arguments.</summary>
public sealed class RectangleArgs
{
    [Description("Width of the rectangle")]
    public double Width { get; set; }

    [Description("Height of the rectangle")]
    public double Height { get; set; }
}
