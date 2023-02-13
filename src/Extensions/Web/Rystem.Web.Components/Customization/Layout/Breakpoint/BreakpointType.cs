using System.Globalization;
using System.Text;

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
        Fluid,
        Every
    }
    public static class BreakpointExtensions
    {
        public static string ToBoostrapBreakpoint(this BreakpointType type, string formattedInput)
        {
            switch (type)
            {
                case BreakpointType.Small:
                    return string.Format(formattedInput, "-sm");
                case BreakpointType.Medium:
                    return string.Format(formattedInput, "-md");
                case BreakpointType.Large:
                    return string.Format(formattedInput, "-lg");
                case BreakpointType.ExtraLarge:
                    return string.Format(formattedInput, "-xl");
                case BreakpointType.ExtraExtraLarge:
                    return string.Format(formattedInput, "-xxl");
                case BreakpointType.Fluid:
                    return string.Format(formattedInput, "-fluid");
                default:
                    return string.Format(formattedInput, string.Empty);
            }
        }
    }
}
