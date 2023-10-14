namespace Rystem.Api
{
    public interface IParameterManager
    {
        Type Type { get; }
        Task<object> ReadAsync();
        Task WriteAsync(object value);
    }
}
