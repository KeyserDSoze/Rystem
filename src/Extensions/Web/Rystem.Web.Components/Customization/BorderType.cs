namespace Rystem.Web.Components.Customization
{
    public enum BorderType
    {
        Normal,
        None,
        Everything
    }
    public static class BorderExtensions
    {
        public static string ToBootstrapBorder(this BorderType type, string formattedInput)
        {
            switch (type)
            {
                case BorderType.Normal:
                    return string.Format(formattedInput, "-borderless");
                case BorderType.Everything:
                    return string.Format(formattedInput, "-bordered");
                default:
                    return string.Format(formattedInput, string.Empty);
            }
        }
    }
}
