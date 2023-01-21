namespace System.Reflection
{
    public static class PropertyHandlerExtensions
    {
        public static TypeShowcase ToShowcase(this Type type, params IFurtherParameter[] furtherParameters)
            => PropertyHandler.Instance.GetEntity(type, furtherParameters);
        public static TypeShowcase ToShowcase<T>(this T? entity, params IFurtherParameter[] furtherParameters)
            => (entity?.GetType() ?? typeof(T)).ToShowcase(furtherParameters);
    }
}
