using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace System
{
    public static class EnumExtensions
    {
        public static TEnum ToEnum<TEnum>(this Enum source)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), source.ToString(), true);
        }
        public static TEnum ToEnum<TEnum>(this string source)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), source, true);
        }
        public static string GetDisplayName(this Enum source)
        {
            var sourceAsString = source.ToString();
            return source.GetType().GetMember(sourceAsString)[0].GetCustomAttribute<DisplayAttribute>()?.GetName() ?? sourceAsString;
        }
    }
}
