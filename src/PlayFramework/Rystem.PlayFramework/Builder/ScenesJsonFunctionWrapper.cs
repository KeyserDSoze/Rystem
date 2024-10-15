using System.Text.RegularExpressions;

namespace Rystem.PlayFramework
{
    internal sealed class ScenesJsonFunctionWrapper
    {
        public List<Action<IChatClientToolBuilder>> Functions { get; set; } = new();
        public required List<Regex> AvailableApiPath { get; set; } = new();
    }
}
