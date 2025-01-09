namespace Microsoft.Extensions.DependencyInjection
{
    internal static class AnyOfExtensions
    {
        public static string? AsString(this AnyOf<string, Enum>? name)
        {
            if (name == null)
                return null;
            else
            {
                return name.Match(x => x, x => x?.GetDisplayName());
            }
        }
    }
}
