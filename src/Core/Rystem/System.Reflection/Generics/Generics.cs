using System.Collections.Concurrent;
namespace System.Reflection
{
    public static class Generics
    {
        private static readonly ConcurrentDictionary<string, MethodInfoWrapper> MethodsCache = new();
        private static readonly ConcurrentDictionary<string, StaticMethodInfoWrapper> StaticMethodsCache = new();

        public static StaticMethodInfoWrapper WithStatic<TContainer>(string methodName, params Type[] generics)
            => WithStatic(typeof(TContainer), methodName, generics);
        [Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Bypass is sure in this case.")]
        public static StaticMethodInfoWrapper WithStatic(Type containerType, string methodName, params Type[] generics)
        {
            string key = $"{containerType.Name}_{methodName}_{string.Join("_", generics.Select(x => x.FullName))}";
            if (StaticMethodsCache.ContainsKey(key))
                return StaticMethodsCache[key];
            var method = containerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(x => x.Name == methodName && x.IsGenericMethod && x.GetGenericArguments().Length == generics.Length)!;
            method = method.MakeGenericMethod(generics);
            StaticMethodsCache.TryAdd(key, new(method));
            return StaticMethodsCache[key];
        }
        public static MethodInfoWrapper With<TContainer>(string methodName, params Type[] generics)
            => With(typeof(TContainer), methodName, generics);
        [Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Bypass is sure in this case.")]
        public static MethodInfoWrapper With(Type containerType, string methodName, params Type[] generics)
        {
            string key = $"{containerType.Name}_{methodName}_{string.Join("_", generics.Select(x => x.FullName))}";
            if (MethodsCache.ContainsKey(key))
                return MethodsCache[key];
            var method = containerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == methodName && x.IsGenericMethod && x.GetGenericArguments().Length == generics.Length)!;
            method = method.MakeGenericMethod(generics);
            MethodsCache.TryAdd(key, new(method));
            return MethodsCache[key];
        }
    }
}