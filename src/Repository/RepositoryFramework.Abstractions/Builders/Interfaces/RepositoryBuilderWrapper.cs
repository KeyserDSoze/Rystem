namespace RepositoryFramework
{
    public sealed class RepositoryBuilderWrapper<TRepositoryBuilder, TOptions>
    {
        public TRepositoryBuilder Builder { get; }
        public TOptions Options { get; }
        public RepositoryBuilderWrapper(TRepositoryBuilder builder, TOptions options)
        {
            Builder = builder;
            Options = options;
        }
    }
}
