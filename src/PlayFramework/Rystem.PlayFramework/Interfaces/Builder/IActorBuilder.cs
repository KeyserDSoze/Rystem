namespace Rystem.OpenAi.Actors
{
    public interface IActorBuilder
    {
        IActorBuilder AddActor<T>() where T : class, IActor;
        public IActorBuilder AddActor(string role);
    }
}
