/**
 * Multi-modal content item for PlayFramework requests.
 * Matches C# ContentItem contract.
 */
export interface ContentItem {
    /**
     * Content type: "text", "image", "audio", "video", "file", "uri"
     */
    type: "text" | "image" | "audio" | "video" | "file" | "uri";

    /**
     * Text content (for type="text")
     */
    text?: string;

    /**
     * Base64-encoded data (for type="image", "audio", "video", "file")
     */
    data?: string;

    /**
     * URI/URL (for type="image", "audio", "video", "file", "uri")
     */
    uri?: string;

    /**
     * Media type (MIME type)
     */
    mediaType?: string;

    /**
     * Optional name/filename.
     */
    name?: string;
}
