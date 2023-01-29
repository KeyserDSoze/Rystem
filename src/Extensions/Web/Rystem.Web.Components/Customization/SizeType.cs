namespace Rystem.Web.Components.Customization
{
    public enum SizeType
    {
        Medium,
        Large,
        Small
    }
    public static class SizeTypeExtensions
    {
        public static string ToBootstrapSize(this SizeType size)
        {
            switch (size)
            {
                case SizeType.Large:
                    return "-lg";
                case SizeType.Small:
                    return "-sm";
                default:
                    return string.Empty;
            }
        }
    }
}
