/**
 * PKCE (Proof Key for Code Exchange) Generator for OAuth 2.0
 * RFC 7636: https://tools.ietf.org/html/rfc7636
 * 
 * NOTE: Storage is now handled by PkceStorageService (decorator pattern)
 * These functions only handle cryptographic operations (generation, hashing)
 */

/**
 * Generate a cryptographically secure random code_verifier
 * @returns Base64URL-encoded random string (64 bytes = ~86 characters)
 */
export function generateCodeVerifier(): string {
    const array = new Uint8Array(64);
    crypto.getRandomValues(array);
    return base64UrlEncode(array);
}

/**
 * Generate code_challenge from code_verifier using SHA-256
 * @param verifier The code_verifier string
 * @returns Base64URL-encoded SHA-256 hash
 */
export async function generateCodeChallenge(verifier: string): Promise<string> {
    const encoder = new TextEncoder();
    const data = encoder.encode(verifier);
    const hashBuffer = await crypto.subtle.digest('SHA-256', data);
    const hashArray = new Uint8Array(hashBuffer);
    return base64UrlEncode(hashArray);
}

/**
 * Base64URL encode (no padding, URL-safe)
 * @param input Byte array to encode
 * @returns Base64URL-encoded string
 */
function base64UrlEncode(input: Uint8Array): string {
    const base64 = btoa(String.fromCharCode(...input));
    return base64
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=/g, '');
}
