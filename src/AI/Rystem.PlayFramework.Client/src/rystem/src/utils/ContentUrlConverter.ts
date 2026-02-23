import type { AIContent } from '../models/ClientInteractionResult';

/**
 * Utility class for converting AIContent (base64 data) to Blob URLs for browser display.
 * 
 * @example
 * ```typescript
 * const content: AIContent = { type: "data", data: "...", mediaType: "image/jpeg" };
 * 
 * // Convert to Blob URL
 * const url = ContentUrlConverter.toBlobUrl(content);
 * 
 * // Use in <img> tag
 * <img src={url} alt="Image" />
 * 
 * // Cleanup when done
 * ContentUrlConverter.revokeUrl(url);
 * ```
 */
export class ContentUrlConverter {
    private static urlCache = new Map<string, string>();

    /**
     * Converts AIContent with data (Base64) to a Blob object.
     * 
     * @param content - AIContent with data field
     * @returns Blob or null if no data present
     */
    static toBlob(content: AIContent): Blob | null {
        if (!content.data) return null;

        try {
            // Decode base64 to binary string
            const byteString = atob(content.data);
            const bytes = new Uint8Array(byteString.length);

            for (let i = 0; i < byteString.length; i++) {
                bytes[i] = byteString.charCodeAt(i);
            }

            return new Blob([bytes], { type: content.mediaType || 'application/octet-stream' });
        } catch (error) {
            console.error('Failed to decode base64 data:', error);
            return null;
        }
    }

    /**
     * Converts AIContent to a blob: URL that can be used in <img>, <audio>, <video>, etc.
     * 
     * @param content - AIContent with data field
     * @param cacheKey - Optional cache key for reusing URLs
     * @returns Blob URL string or null if conversion fails
     */
    static toBlobUrl(content: AIContent, cacheKey?: string): string | null {
        // For text content, return null (not displayable as media)
        if (content.type === 'text') {
            return null;
        }

        // Check cache first
        if (cacheKey && this.urlCache.has(cacheKey)) {
            return this.urlCache.get(cacheKey)!;
        }

        const blob = this.toBlob(content);
        if (!blob) return null;

        const url = URL.createObjectURL(blob);

        // Store in cache if key provided
        if (cacheKey) {
            this.urlCache.set(cacheKey, url);
        }

        return url;
    }

    /**
     * Revokes a blob URL to free memory. Always call this when done using the URL.
     * 
     * @param url - Blob URL to revoke
     * @param cacheKey - Optional cache key to remove from cache
     */
    static revokeUrl(url: string, cacheKey?: string): void {
        if (url.startsWith('blob:')) {
            URL.revokeObjectURL(url);
        }

        if (cacheKey) {
            this.urlCache.delete(cacheKey);
        }
    }

    /**
     * Clears all cached blob URLs and revokes them.
     */
    static clearCache(): void {
        this.urlCache.forEach(url => {
            if (url.startsWith('blob:')) {
                URL.revokeObjectURL(url);
            }
        });
        this.urlCache.clear();
    }

    /**
     * Downloads AIContent as a file.
     * 
     * @param content - AIContent to download
     * @param filename - Filename to use (defaults to "download" + extension)
     */
    static downloadAsFile(content: AIContent, filename?: string): void {
        const blob = this.toBlob(content);
        if (!blob) {
            console.error('Cannot download: no blob data');
            return;
        }

        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;

        // Generate filename with proper extension
        const extension = this.getFileExtension(content.mediaType);
        a.download = filename || `download${extension}`;

        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }

    /**
     * Gets the file extension from mediaType.
     * 
     * @param mediaType - MIME type (e.g., "image/jpeg")
     * @returns File extension (e.g., ".jpg") or empty string
     */
    static getFileExtension(mediaType?: string): string {
        if (!mediaType) return '';

        const extensionMap: Record<string, string> = {
            'image/jpeg': '.jpg',
            'image/png': '.png',
            'image/gif': '.gif',
            'image/webp': '.webp',
            'image/svg+xml': '.svg',
            'audio/mpeg': '.mp3',
            'audio/wav': '.wav',
            'audio/ogg': '.ogg',
            'video/mp4': '.mp4',
            'video/webm': '.webm',
            'video/ogg': '.ogv',
            'application/pdf': '.pdf',
            'application/json': '.json',
            'text/plain': '.txt',
            'text/html': '.html',
            'text/css': '.css',
            'application/javascript': '.js',
        };

        return extensionMap[mediaType] || '';
    }
}
