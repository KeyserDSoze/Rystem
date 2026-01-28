/**
 * Mock URL Service for unit testing
 * 
 * This is a simple in-memory implementation for testing purposes.
 * Allows setting URL parameters programmatically without browser navigation.
 * 
 * @example
 * ```typescript
 * import { MockUrlService } from './MockUrlService';
 * 
 * const mockUrlService = new MockUrlService();
 * mockUrlService.setParam('code', 'test-auth-code-12345');
 * mockUrlService.setParam('state', 'microsoft');
 * 
 * setupSocialLogin(x => {
 *     x.urlService = mockUrlService;
 *     // ... rest of test config
 * });
 * 
 * // In your test
 * expect(mockUrlService.getSearchParam('code')).toBe('test-auth-code-12345');
 * ```
 */

import type { IUrlService } from './IUrlService';

/**
 * Mock URL Service for testing
 * Stores parameters in-memory without browser URL manipulation
 */
export class MockUrlService implements IUrlService {
    private params: Map<string, string> = new Map();

    /**
     * Set a URL parameter (for testing purposes)
     * @param key - Parameter name
     * @param value - Parameter value
     * 
     * @example
     * ```typescript
     * mockUrlService.setParam('code', 'test-code');
     * mockUrlService.setParam('state', 'google');
     * ```
     */
    setParam(key: string, value: string): void {
        this.params.set(key, value);
    }

    /**
     * Set multiple parameters at once
     * @param params - Object with key-value pairs
     * 
     * @example
     * ```typescript
     * mockUrlService.setParams({
     *     code: 'test-code',
     *     state: 'microsoft'
     * });
     * ```
     */
    setParams(params: Record<string, string>): void {
        Object.entries(params).forEach(([key, value]) => {
            this.params.set(key, value);
        });
    }

    /**
     * Remove a parameter
     * @param key - Parameter name to remove
     */
    removeParam(key: string): void {
        this.params.delete(key);
    }

    /**
     * Clear all parameters
     */
    clear(): void {
        this.params.clear();
    }

    /**
     * Get a single URL parameter value
     * @param key - Parameter name
     * @returns Parameter value or null if not found
     */
    getSearchParam(key: string): string | null {
        return this.params.get(key) || null;
    }

    /**
     * Get all URL parameters
     * @returns URLSearchParams object with all parameters
     */
    getAllSearchParams(): URLSearchParams {
        const urlParams = new URLSearchParams();
        this.params.forEach((value, key) => {
            urlParams.set(key, value);
        });
        return urlParams;
    }

    /**
     * Check if a parameter exists
     * @param key - Parameter name
     * @returns True if parameter exists
     */
    hasParam(key: string): boolean {
        return this.params.has(key);
    }

    /**
     * Get all parameter keys
     * @returns Array of parameter names
     */
    getParamKeys(): string[] {
        return Array.from(this.params.keys());
    }

    /**
     * Get count of parameters
     * @returns Number of stored parameters
     */
    getParamCount(): number {
        return this.params.size;
    }
}
