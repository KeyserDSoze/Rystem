/**
 * Multi-modal content item for PlayFramework requests.
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
    base64Data?: string;

    /**
     * URI/URL (for type="image", "audio", "video", "file", "uri")
     */
    uri?: string;

    /**
     * Media type (MIME type)
     */
    mediaType?: string;
}
