namespace Rystem.Test.TestApi.Models
{
    public sealed class ServiceWrapper
    {
        public SingletonService SingletonService { get; set; }
        public Singleton2Service Singleton2Service { get; set; }
        public ScopedService ScopedService { get; set; }
        public Scoped2Service Scoped2Service { get; set; }
        public TransientService TransientService { get; set; }
        public Transient2Service Transient2Service { get; set; }
        public AddedService? AddedService { get; set; }
    }
}
