import { IStorageService } from "./IStorageService";

/**
 * PKCE storage service (decorator pattern)
 * Adds domain logic (key prefixes, validation) on top of generic storage
 */
export class PkceStorageService {
    private readonly prefix = 'rystem_pkce_';

    constructor(private storage: IStorageService) {}

    /**
     * Store PKCE code verifier for a provider
     * @param provider Provider name (microsoft, google, etc.)
     * @param codeVerifier PKCE code verifier (43+ chars, Base64URL)
     */
    storeCodeVerifier(provider: string, codeVerifier: string): void {
        if (!codeVerifier || codeVerifier.length < 43) {
            console.warn(`PkceStorageService: Invalid code verifier for ${provider}`);
            return;
        }

        const key = this.getKey(provider, 'verifier');
        this.storage.set(key, codeVerifier);
    }

    /**
     * Get PKCE code verifier for a provider
     * @param provider Provider name
     * @returns Code verifier or null if not found/invalid
     */
    getCodeVerifier(provider: string): string | null {
        const key = this.getKey(provider, 'verifier');
        const value = this.storage.get(key);

        // Validation: PKCE verifier must be at least 43 characters (RFC 7636)
        if (value && value.length < 43) {
            console.warn(`PkceStorageService: Invalid code verifier for ${provider} (length: ${value.length})`);
            this.removeCodeVerifier(provider);
            return null;
        }

        return value;
    }

    /**
     * Get and remove PKCE code verifier (consume once)
     * Used during token exchange to retrieve and cleanup
     */
    getAndRemoveCodeVerifier(provider: string): string | null {
        const verifier = this.getCodeVerifier(provider);
        if (verifier) {
            this.removeCodeVerifier(provider);
        }
        return verifier;
    }

    /**
     * Remove PKCE code verifier for a provider
     */
    removeCodeVerifier(provider: string): void {
        const key = this.getKey(provider, 'verifier');
        this.storage.remove(key);
    }

    /**
     * Check if PKCE code verifier exists for a provider
     */
    hasCodeVerifier(provider: string): boolean {
        const key = this.getKey(provider, 'verifier');
        return this.storage.has(key);
    }

    /**
     * Store PKCE code challenge for a provider (optional, for debugging)
     */
    storeCodeChallenge(provider: string, codeChallenge: string): void {
        const key = this.getKey(provider, 'challenge');
        this.storage.set(key, codeChallenge);
    }

    /**
     * Get PKCE code challenge for a provider
     */
    getCodeChallenge(provider: string): string | null {
        const key = this.getKey(provider, 'challenge');
        return this.storage.get(key);
    }

    /**
     * Remove PKCE code challenge for a provider
     */
    removeCodeChallenge(provider: string): void {
        const key = this.getKey(provider, 'challenge');
        this.storage.remove(key);
    }

    /**
     * Clear all PKCE data for a provider
     */
    clearAll(provider: string): void {
        this.removeCodeVerifier(provider);
        this.removeCodeChallenge(provider);
    }

    /**
     * Build storage key with prefix
     */
    private getKey(provider: string, type: 'verifier' | 'challenge'): string {
        return `${this.prefix}${provider}_${type}`;
    }
}
