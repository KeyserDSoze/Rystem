/**
 * Generic storage service interface (key-value storage)
 * Allows different infrastructure implementations (localStorage, secure storage, Redis, etc.)
 */
export interface IStorageService {
    /**
     * Get value by key
     * @param key Storage key
     * @returns Value or null if not found
     */
    get(key: string): string | null;

    /**
     * Set value for key
     * @param key Storage key
     * @param value Value to store
     */
    set(key: string, value: string): void;

    /**
     * Remove value by key
     * @param key Storage key
     */
    remove(key: string): void;

    /**
     * Check if key exists
     * @param key Storage key
     * @returns True if key exists
     */
    has(key: string): boolean;

    /**
     * Clear all stored values (optional)
     */
    clear?(): void;
}
