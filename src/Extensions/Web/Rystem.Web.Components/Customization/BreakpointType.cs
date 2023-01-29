namespace Rystem.Web.Components.Customization
{
    public enum BreakpointType
    {
        None,
        Small,
        Medium,
        Large,
        ExtraLarge,
        ExtraExtraLarge,
        Every
    }
    public static class BreakpointExtensions
    {
        public static string ToBoostrapBreakpoint(this BreakpointType type)
        {
            switch (type)
            {
                case BreakpointType.Small:
                    return "-sm";
                case BreakpointType.Medium:
                    return "-md";
                case BreakpointType.Large:
                    return "-lg";
                case BreakpointType.ExtraLarge:
                    return "-xl";
                case BreakpointType.ExtraExtraLarge:
                    return "-xxl";
                default:
                    return string.Empty;
            }
        }
    }
}
