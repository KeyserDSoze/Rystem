using System.Text.Json.Serialization;
using Rystem.PlayFramework;

namespace Rystem.OpenAi.Actors
{
    internal static class ScenesBuilderHelper
    {
        public static List<Action<IChatClientToolBuilder>> ScenesAsFunctions { get; } = [];
        public static Dictionary<string, ScenesJsonFunctionWrapper> FunctionsForEachScene { get; } = [];
        public static Dictionary<string, Dictionary<string, Func<Dictionary<string, string>, HttpBringer, ValueTask>>> Actions { get; } = [];
        public static Dictionary<string, Func<HttpBringer, ValueTask>> Calls { get; } = [];
    }
}
