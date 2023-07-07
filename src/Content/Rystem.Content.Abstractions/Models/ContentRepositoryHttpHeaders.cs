namespace Rystem.Content
{
    public class ContentRepositoryHttpHeaders
    {
        /// <summary>
        /// The MIME content type of the file.
        /// </summary>
        public string? ContentType { get; set; }
        /// <summary>
        /// An MD5 hash of the file content. This hash is used to verify the
        /// integrity of the blob during transport.  When this header is
        /// specified, the storage service checks the hash that has arrived
        /// with the one that was sent. If the two hashes do not match, the
        /// operation will fail with error code 400 (Bad Request).
        /// </summary>
        public byte[]? ContentHash { get; set; }
        /// <summary>
        /// Specifies which content encodings have been applied to the file.
        /// This value is returned to the client when the Get Blob operation
        /// is performed on the blob resource. The client can use this value
        /// when returned to decode the blob content.
        /// </summary>
        public string? ContentEncoding { get; set; }
        /// <summary>
        /// Specifies the natural languages used by this resource.
        /// </summary>
        public string? ContentLanguage { get; set; }
        /// <summary>
        /// Conveys additional information about how to process the response
        /// payload, and also can be used to attach additional metadata.  For
        /// example, if set to attachment, it indicates that the user-agent
        /// should not display the response, but instead show a Save As dialog
        /// with a filename other than the blob name specified.
        /// </summary>
        public string? ContentDisposition { get; set; }
        /// <summary>
        /// Specify directives for caching mechanisms.
        /// </summary>
        public string? CacheControl { get; set; }
    }
}
