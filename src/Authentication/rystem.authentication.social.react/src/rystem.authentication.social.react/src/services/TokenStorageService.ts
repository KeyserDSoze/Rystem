import { IStorageService } from "./IStorageService";
import { Token } from "../models/Token";

/**
 * Token storage service (decorator pattern)
 * Adds domain logic (expiry check, JSON serialization) on top of generic storage
 */
export class TokenStorageService {
private readonly tokenKey = 'socialUserToken';  // Backward compatible key
private readonly expiryKey = 'socialUserToken_expiry';

constructor(private storage: IStorageService) {}

    /**
     * Save social authentication token
     * @param token Token with expiry information (Date)
     */
    saveToken(token: Token): void {
        try {
            this.storage.set(this.tokenKey, JSON.stringify(token));
            
            // Store expiry separately for quick validation
            if (token.expiresIn) {
                this.storage.set(this.expiryKey, token.expiresIn.toISOString());
            }
        } catch (error) {
            console.error('TokenStorageService: Error saving token', error);
        }
    }

    /**
     * Get social authentication token
     * Returns null if not found or expired
     */
    getToken(): Token | null {
        try {
            // Check expiry first (faster than parsing JSON)
            if (this.isExpired()) {
                this.clearToken();
                return null;
            }

            const raw = this.storage.get(this.tokenKey);
            if (!raw) {
                return null;
            }

            const token = JSON.parse(raw) as Token;
            
            // Convert ISO string back to Date
            if (token.expiresIn) {
                token.expiresIn = new Date(token.expiresIn);
            }

            return token;
        } catch (error) {
            console.error('TokenStorageService: Error getting token', error);
            this.clearToken();
            return null;
        }
    }

    /**
     * Check if token exists (not expired)
     */
    hasToken(): boolean {
        return !this.isExpired() && this.storage.has(this.tokenKey);
    }

    /**
     * Clear token from storage
     */
    clearToken(): void {
        this.storage.remove(this.tokenKey);
        this.storage.remove(this.expiryKey);
    }

    /**
     * Check if token is expired
     */
    private isExpired(): boolean {
        const expiryRaw = this.storage.get(this.expiryKey);
        if (!expiryRaw) {
            return false; // No expiry set = no token or never expires
        }

        try {
            const expiryDate = new Date(expiryRaw);
            return expiryDate <= new Date();
        } catch (error) {
            console.error('TokenStorageService: Error checking expiry', error);
            return true; // If can't parse, consider expired
        }
    }
}
