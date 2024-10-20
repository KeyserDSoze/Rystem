namespace Rystem.PlayFramework
{
    public interface IScenesBuilder
    {
        IScenesBuilder Configure(Action<SceneManagerSettings> settings);
        IScenesBuilder AddMainActor(string role, bool playInEveryScene);
        IScenesBuilder AddMainActor<T>(bool playInEveryScene) where T : class, IActor;
        IScenesBuilder AddMainActor(Func<SceneContext, string> action, bool playInEveryScene);
        IScenesBuilder AddMainActor(Func<SceneContext, CancellationToken, Task<string>> action, bool playInEveryScene);
        IScenesBuilder AddScene(Action<ISceneBuilder> builder);
    }
}
