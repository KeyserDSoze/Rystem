namespace Microsoft.AspNetCore.Builder
{
    public sealed class ApiParameterLocationAttribute : Attribute
    {
        public ApiParameterLocationAttribute(ApiParameterLocation location)
        {
            Location = location;
        }
        public ApiParameterLocationAttribute(ApiParameterLocation location, int position)
        {
            Location = location;
            Position = position;
        }
        public ApiParameterLocation Location { get; }
        public int? Position { get; }
    }
}
