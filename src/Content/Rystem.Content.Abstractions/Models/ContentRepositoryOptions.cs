namespace Rystem.Content
{
    public class ContentRepositoryOptions
    {
        public ContentRepositoryHttpHeaders? HttpHeaders { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
    }
}
