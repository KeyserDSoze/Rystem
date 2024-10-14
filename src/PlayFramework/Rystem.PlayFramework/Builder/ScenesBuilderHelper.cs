namespace Rystem.PlayFramework
{
    internal static class ScenesBuilderHelper
    {
        public static List<Action<IChatClientToolBuilder>> ScenesAsFunctions { get; } = [];
        public static Dictionary<string, ScenesJsonFunctionWrapper> FunctionsForEachScene { get; } = [];
        public static Dictionary<string, Dictionary<string, Func<Dictionary<string, string>, HttpBringer, ValueTask>>> HttpActions { get; } = [];
        public static Dictionary<string, Dictionary<string, Func<Dictionary<string, string>, ServiceBringer, ValueTask>>> ServiceActions { get; } = [];
        public static Dictionary<string, Func<HttpBringer, ValueTask>> HttpCalls { get; } = [];
        public static Dictionary<string, Func<IServiceProvider, ServiceBringer, Task<object>>> ServiceCalls { get; } = [];
    }
}
