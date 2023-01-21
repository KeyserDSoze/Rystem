namespace System
{
    public sealed record TryResponse<T>(T? Entity, Exception? Exception = null)
    {
        public static implicit operator T(TryResponse<T> response)
            => response.Entity!;
        public static implicit operator bool(TryResponse<T> response)
           => response.Entity is not null;
    }
}
