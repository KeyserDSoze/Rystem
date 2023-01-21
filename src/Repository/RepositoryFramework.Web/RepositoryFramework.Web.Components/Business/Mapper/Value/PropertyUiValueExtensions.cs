namespace RepositoryFramework.Web.Components
{
    public static class PropertyUiValueExtensions
    {
        public static bool HasValues(this PropertyUiSettings? retrievd)
            => retrievd?.Values != null;
    }
}
