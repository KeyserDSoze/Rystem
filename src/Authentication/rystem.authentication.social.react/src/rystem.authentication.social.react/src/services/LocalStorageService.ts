import { IStorageService } from "./IStorageService";

/**
 * Default storage implementation using browser localStorage
 * Works for web applications (browser environment)
 * For React Native, provide a custom IStorageService implementation
 */
export class LocalStorageService implements IStorageService {
    /**
     * Check if localStorage is available (browser environment)
     */
    private isAvailable(): boolean {
        return typeof window !== 'undefined' && typeof localStorage !== 'undefined';
    }

    /**
     * Get value from localStorage
     */
    get(key: string): string | null {
        if (!this.isAvailable()) {
            console.warn('LocalStorageService: localStorage not available (React Native or SSR?)');
            return null;
        }
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
        if (!this.isAvailable()) {
            console.warn('LocalStorageService: localStorage not available (React Native or SSR?)');
            return;
        }
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
        if (!this.isAvailable()) {
            console.warn('LocalStorageService: localStorage not available (React Native or SSR?)');
            return;
        }
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
        if (!this.isAvailable()) {
            return false;
        }
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
        if (!this.isAvailable()) {
            console.warn('LocalStorageService: localStorage not available (React Native or SSR?)');
            return;
        }
        try {
            localStorage.clear();
        } catch (error) {
            console.error('LocalStorageService: Error clearing storage', error);
        }
    }
}
