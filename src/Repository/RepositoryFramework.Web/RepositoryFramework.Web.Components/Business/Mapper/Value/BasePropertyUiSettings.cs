namespace RepositoryFramework.Web
{
    public abstract class BasePropertyUiSettings
    {
        public object? Default { get; set; }
        public object? DefaultKey { get; set; }
        public Func<object, object?> ValueRetriever { get; set; }
        public bool IsMultiple { get; set; }
        public bool HasTextEditor { get; set; }
        public int MinHeight { get; set; }
        public Func<object, string>? LabelComparer { get; set; }
    }
}
