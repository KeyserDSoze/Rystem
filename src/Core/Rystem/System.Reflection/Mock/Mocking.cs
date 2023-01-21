namespace System.Reflection
{
    public static class Mocking
    {
        public static Type? Mock(this Type type)
            => MockedAssembly.Instance.GetMockedType(type);
        public static Type? Mock<T>(this T entity)
            => MockedAssembly.Instance.GetMockedType<T>();
        public static Type? Mock<T>()
            => MockedAssembly.Instance.GetMockedType<T>();
        public static object CreateInstance(this Type type, params object[]? args) 
            => MockedAssembly.Instance.CreateInstance(type, args);
        public static T CreateInstance<T>(this T entity, params object[]? args)
            => MockedAssembly.Instance.CreateInstance<T>(args);
        public static T CreateInstance<T>(params object[]? args)
            => MockedAssembly.Instance.CreateInstance<T>(args);
    }
}
