namespace Rystem.OpenAi.Actors
{
    public interface IScenesBuilder
    {
        IScenesBuilder Configure(Action<SceneManagerSettings> settings);
        IScenesBuilder AddScene(Action<ISceneBuilder> builder);
    }
}
