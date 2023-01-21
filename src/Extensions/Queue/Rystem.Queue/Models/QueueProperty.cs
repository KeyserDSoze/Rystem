namespace Rystem.Queue
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed", Justification = "T is needed for injection.")]
    public sealed class QueueProperty<T>
    {
        /// <summary>
        /// Maximum number of items to fire the dequeueing.
        /// </summary>
        public int MaximumBuffer { get; set; } = 5000;
        /// <summary>
        /// Maximum time window to fire the dequeueing.
        /// </summary>
        public string MaximumRetentionCronFormat { get; set; } = "*/1 * * * *";
        /// <summary>
        /// Time window for checking the number of items and maximum time window.
        /// </summary>
        public string BackgroundJobCronFormat { get; set; } = "*/1 * * * *";
    }
}