import { IStorageService } from "./IStorageService";

/**
 * Default storage implementation using browser localStorage
 * Works for web applications (browser environment)
 */
export class LocalStorageService implements IStorageService {
    /**
     * Get value from localStorage
     */
    get(key: string): string | null {
        try {
            return localStorage.getItem(key);
        } catch (error) {
            console.error(`LocalStorageService: Error getting key "${key}"`, error);
            return null;
        }
    }

    /**
     * Set value in localStorage
     */
    set(key: string, value: string): void {
        try {
            localStorage.setItem(key, value);
        } catch (error) {
            console.error(`LocalStorageService: Error setting key "${key}"`, error);
        }
    }

    /**
     * Remove value from localStorage
     */
    remove(key: string): void {
        try {
            localStorage.removeItem(key);
        } catch (error) {
            console.error(`LocalStorageService: Error removing key "${key}"`, error);
        }
    }

    /**
     * Check if key exists in localStorage
     */
    has(key: string): boolean {
        try {
            return localStorage.getItem(key) !== null;
        } catch (error) {
            console.error(`LocalStorageService: Error checking key "${key}"`, error);
            return false;
        }
    }

    /**
     * Clear all localStorage
     */
    clear(): void {
        try {
            localStorage.clear();
        } catch (error) {
            console.error('LocalStorageService: Error clearing storage', error);
        }
    }
}
