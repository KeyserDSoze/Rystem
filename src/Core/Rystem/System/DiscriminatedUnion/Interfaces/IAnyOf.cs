namespace System
{
    public interface IAnyOf
    {
        object? Value { get; set; }
        int Index { get; }
        T? Get<T>();
        bool Is<T>();
        bool Is<T>(out T? entity);
        Type? GetCurrentType();
    }
}
