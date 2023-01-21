namespace RepositoryFramework.InMemory
{
    /// <summary>
    /// Mapping for your exception, you may use a percentage from 0.000000000000000000000000001% and 100% with
    /// a value betweeen 0.000000000000000000000000001 and 100 in Percentage property.
    /// </summary>
    public class ExceptionOdds
    {
        public double Percentage { get; set; }
        public Exception? Exception { get; set; }
    }
}