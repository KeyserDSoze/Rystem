namespace Rystem.PlayFramework
{
    public sealed class SceneRequestSettings
    {
        internal List<string>? ScenesToAvoid { get; set; }
        public SceneRequestSettings AvoidScene(string name)
        {
            ScenesToAvoid ??= [];
            ScenesToAvoid.Add(name);
            return this;
        }
        internal Dictionary<object, object>? Properties { get; set; }
        public SceneRequestSettings AddProperty<TKey, T>(TKey key, T value)
        {
            Properties ??= [];
            Properties.Add(key!, value!);
            return this;
        }
    }
}
