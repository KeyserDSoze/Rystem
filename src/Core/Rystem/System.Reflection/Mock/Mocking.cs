namespace System.Reflection
{
    public static class Mocking
    {
        public static Type? Mock(this Type type, Action<MockingConfiguration>? configuration = null)
            => MockedAssembly.Instance.GetMockedType(type, configuration);
        public static Type? Mock<T>(this T entity, Action<MockingConfiguration>? configuration = null)
            => MockedAssembly.Instance.GetMockedType<T>(configuration);
        public static Type? Mock<T>(Action<MockingConfiguration>? configuration = null)
            => MockedAssembly.Instance.GetMockedType<T>(configuration);
        public static object CreateInstance(this Type type, Action<MockingConfiguration>? configuration, params object[]? args)
            => MockedAssembly.Instance.CreateInstance(type, configuration, args);
        public static T CreateInstance<T>(this T entity, Action<MockingConfiguration>? configuration, params object[]? args)
            => MockedAssembly.Instance.CreateInstance<T>(configuration, args);
        public static T CreateInstance<T>(Action<MockingConfiguration>? configuration, params object[]? args)
            => MockedAssembly.Instance.CreateInstance<T>(configuration, args);
        public static object CreateInstance(this Type type, params object[]? args)
            => MockedAssembly.Instance.CreateInstance(type, null, args);
        public static T CreateInstance<T>(this T entity, params object[]? args)
            => MockedAssembly.Instance.CreateInstance<T>(null, args);
        public static T CreateInstance<T>(params object[]? args)
            => MockedAssembly.Instance.CreateInstance<T>(null, args);
    }
    public sealed class MockingConfiguration
    {
        public bool IsSealed { get; set; } = true;
        public bool CreateNewOneIfExists { get; set; }
    }
}
