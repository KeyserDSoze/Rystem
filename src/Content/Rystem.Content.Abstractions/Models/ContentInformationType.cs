namespace Rystem.Content
{
    [Flags]
    public enum ContentInformationType
    {
        None = 0,
        HttpHeaders = 1,
        Metadata = 2,
        Tags = 4,
        All = 7
    }
}
