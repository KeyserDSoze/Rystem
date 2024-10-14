namespace Rystem.PlayFramework
{
    public interface ISceneServiceBuilder<T>
        where T : class
    {
        ISceneServiceBuilder<T> WithMethod(Func<T, Delegate> method);
    }
}
