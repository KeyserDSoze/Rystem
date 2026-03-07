namespace Rystem.PlayFramework.Api.Services;

/// <summary>
/// Implementation of IShapeService using AnyOf parameters.
/// Used to expose the PlayFramework tool-dispatch error when AnyOf is involved.
/// </summary>
public sealed class ShapeService : IShapeService
{
    private readonly ILogger<ShapeService> _logger;

    private static readonly Dictionary<int, string> ShapeCodes = new()
    {
        [1] = "circle",
        [2] = "rectangle",
        [3] = "triangle",
        [4] = "square",
        [5] = "pentagon",
    };

    public ShapeService(ILogger<ShapeService> logger)
    {
        _logger = logger;
    }

    public string DescribeShape(AnyOf<string, int> shapeNameOrCode)
    {
        string shapeName;

        if (shapeNameOrCode.IsT1)
        {
            // It's a numeric code
            var code = shapeNameOrCode.CastT1;
            shapeName = ShapeCodes.TryGetValue(code, out var name) ? name : $"unknown (code {code})";
            _logger.LogInformation("DescribeShape by code: {Code} → {Name}", code, shapeName);
        }
        else
        {
            shapeName = shapeNameOrCode.CastT0?.ToLowerInvariant() ?? "unknown";
            _logger.LogInformation("DescribeShape by name: {Name}", shapeName);
        }

        return shapeName switch
        {
            "circle" => "A circle is a round 2D shape where every point on its boundary is equidistant from the center.",
            "rectangle" => "A rectangle is a 4-sided polygon with four right angles and opposite sides equal.",
            "triangle" => "A triangle is a polygon with three edges and three vertices.",
            "square" => "A square is a rectangle with all four sides equal.",
            "pentagon" => "A pentagon is a polygon with five sides and five angles.",
            _ => $"Shape '{shapeName}' is not in the known catalog."
        };
    }

    public double CalculateArea(AnyOf<CircleArgs, RectangleArgs> shape)
    {
        if (shape.IsT0)
        {
            var circle = shape.CastT0;
            var area = Math.PI * circle.Radius * circle.Radius;
            _logger.LogInformation("CalculateArea circle r={Radius} → {Area}", circle.Radius, area);
            return area;
        }
        else
        {
            var rect = shape.CastT1;
            var area = rect.Width * rect.Height;
            _logger.LogInformation("CalculateArea rectangle {W}×{H} → {Area}", rect.Width, rect.Height, area);
            return area;
        }
    }

    public string GetShapeInfo(AnyOf<string, int> shapeIdentifier, AnyOf<double, string>? areaOverride = null)
    {
        var description = DescribeShape(shapeIdentifier);

        string areaInfo;
        if (areaOverride is null)
        {
            areaInfo = "no area override provided";
        }
        else if (areaOverride.IsT0)
        {
            areaInfo = $"area override = {areaOverride.CastT0:F2}";
        }
        else
        {
            areaInfo = $"area label = '{areaOverride.CastT1}'";
        }

        var result = $"{description} [{areaInfo}]";
        _logger.LogInformation("GetShapeInfo: {Result}", result);
        return result;
    }
}
