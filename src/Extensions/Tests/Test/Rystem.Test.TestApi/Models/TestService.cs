namespace Rystem.Test.TestApi.Models
{
    public abstract class TestService : IDisposable, IAsyncDisposable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public void Dispose()
        {
            Console.WriteLine(nameof(Dispose));
        }

        public ValueTask DisposeAsync()
        {
            Console.WriteLine(nameof(DisposeAsync));
            return ValueTask.CompletedTask;
        }
    }
}
