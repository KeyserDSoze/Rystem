using System.Collections.Concurrent;
namespace System.Reflection
{
    public static class Generics
    {
        private static readonly ConcurrentDictionary<string, MethodInfoWrapper> s_methodsCache = new();
        private static readonly ConcurrentDictionary<string, StaticMethodInfoWrapper> s_staticMethodsCache = new();

        public static StaticMethodInfoWrapper WithStatic<TContainer>(string methodName, params Type[] generics)
            => WithStatic(typeof(TContainer), methodName, generics);
        [Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Bypass is sure in this case.")]
        public static StaticMethodInfoWrapper WithStatic(Type containerType, string methodName, params Type[] generics)
        {
            var key = $"{containerType.Name}_{methodName}_{string.Join("_", generics.Select(x => x.FullName))}";
            if (s_staticMethodsCache.ContainsKey(key))
                return s_staticMethodsCache[key];
            var method = containerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(x => x.Name == methodName && x.IsGenericMethod && x.GetGenericArguments().Length == generics.Length)!;
            method = method.MakeGenericMethod(generics);
            s_staticMethodsCache.TryAdd(key, new(method));
            return s_staticMethodsCache[key];
        }
        public static MethodInfoWrapper With<TContainer>(string methodName, params Type[] generics)
            => With(typeof(TContainer), methodName, generics);
        [Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Bypass is sure in this case.")]
        public static MethodInfoWrapper With(Type containerType, string methodName, params Type[] generics)
        {
            var key = $"{containerType.Name}_{methodName}_{string.Join("_", generics.Select(x => x.FullName))}";
            if (s_methodsCache.ContainsKey(key))
                return s_methodsCache[key];
            var method = containerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == methodName && x.IsGenericMethod && x.GetGenericArguments().Length == generics.Length)!;
            method = method.MakeGenericMethod(generics);
            s_methodsCache.TryAdd(key, new(method));
            return s_methodsCache[key];
        }
    }
}
