namespace RepositoryFramework
{
    public interface IDefaultKey
    {
        internal static string DefaultSeparator = "|||";
        public static void SetDefaultSeparator(string separator)
        {
            DefaultSeparator = separator;
        }
    }
}
