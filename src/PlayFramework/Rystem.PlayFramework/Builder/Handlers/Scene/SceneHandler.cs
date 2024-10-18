using System.Text.RegularExpressions;

namespace Rystem.PlayFramework
{
    internal sealed class SceneHandler
    {
        public Action<IChatClientToolBuilder>? Chooser { get; set; }
        public List<Regex> AvailableApiPath { get; } = [];
        public List<string> Functions { get; } = [];
    }
}
