namespace System
{
    internal sealed class Wrapper(object? entity)
    {
        public object? Entity { get; internal set; } = entity;
    }
}
