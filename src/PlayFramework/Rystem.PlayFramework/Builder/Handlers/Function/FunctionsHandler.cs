namespace Rystem.PlayFramework
{
    internal sealed class FunctionsHandler
    {
        public static FunctionsHandler Instance { get; } = new();
        public FunctionHandler this[string functionName]
        {
            get
            {
                if (!Functions.ContainsKey(functionName))
                    Functions.Add(functionName, new());
                return Functions[functionName];
            }
        }
        public IEnumerable<Action<IChatClientToolBuilder>> FunctionsChooser(string sceneName)
            => Functions.Where(x => x.Value.Chooser != null && x.Value.Scenes.Contains(sceneName)).Select(x => x.Value.Chooser!);
        private Dictionary<string, FunctionHandler> Functions { get; } = [];
    }
}
