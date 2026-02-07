/**
 * Mock Routing Service for unit testing
 * 
 * This is a simple in-memory implementation for testing purposes.
 * Allows setting URL parameters and controlling navigation programmatically.
 * 
 * @example
 * ```typescript
 * import { MockRoutingService } from './MockRoutingService';
 * 
 * const mockRouting = new MockRoutingService();
 * 
 * // Setup test data
 * mockRouting.setSearchParam('code', 'test-auth-code-12345');
 * mockRouting.setSearchParam('state', 'microsoft');
 * mockRouting.setCurrentPath('/account/login?tab=oauth');
 * 
 * setupSocialLogin(x => {
 *     x.routingService = mockRouting;
 *     // ... rest of test config
 * });
 * 
 * // In your test
 * expect(mockRouting.getSearchParam('code')).toBe('test-auth-code-12345');
 * expect(mockRouting.wasNavigateToCalledWith('https://oauth.provider.com')).toBe(true);
 * ```
 */

import type { IRoutingService } from './IRoutingService';

/**
 * Mock Routing Service for testing
 * Stores state in-memory and tracks navigation calls
 */
export class MockRoutingService implements IRoutingService {
    private params: Map<string, string> = new Map();
    private currentPath: string = '/';
    private navigationHistory: string[] = [];
    private replaceHistory: string[] = [];
    private popupCalls: Array<{ url: string; name: string; features: string }> = [];

    // ===== Setup Methods (for tests) =====

    /**
     * Set a URL parameter (for testing OAuth callbacks)
     * @param key - Parameter name
     * @param value - Parameter value
     */
    setSearchParam(key: string, value: string): void {
        this.params.set(key, value);
    }

    /**
     * Set multiple parameters at once
     * @param params - Object with key-value pairs
     */
    setSearchParams(params: Record<string, string>): void {
        Object.entries(params).forEach(([key, value]) => {
            this.params.set(key, value);
        });
    }

    /**
     * Set the current path
     * @param path - Path with query string (e.g., "/dashboard?tab=settings")
     */
    setCurrentPath(path: string): void {
        this.currentPath = path;
    }

    /**
     * Clear all state (for test cleanup)
     */
    reset(): void {
        this.params.clear();
        this.currentPath = '/';
        this.navigationHistory = [];
        this.replaceHistory = [];
        this.popupCalls = [];
    }

    // ===== Verification Methods (for test assertions) =====

    /**
     * Check if navigateTo was called with specific URL
     * @param url - URL to check
     * @returns True if navigateTo was called with this URL
     */
    wasNavigateToCalledWith(url: string): boolean {
        return this.navigationHistory.includes(url);
    }

    /**
     * Get all navigateTo calls
     * @returns Array of URLs passed to navigateTo
     */
    getNavigationHistory(): string[] {
        return [...this.navigationHistory];
    }

    /**
     * Check if navigateReplace was called with specific path
     * @param path - Path to check
     * @returns True if navigateReplace was called with this path
     */
    wasReplaceCalledWith(path: string): boolean {
        return this.replaceHistory.includes(path);
    }

    /**
     * Get all navigateReplace calls
     * @returns Array of paths passed to navigateReplace
     */
    getReplaceHistory(): string[] {
        return [...this.replaceHistory];
    }

    /**
     * Get all openPopup calls
     * @returns Array of popup call parameters
     */
    getPopupCalls(): Array<{ url: string; name: string; features: string }> {
        return [...this.popupCalls];
    }

    // ===== IRoutingService Implementation =====

    getSearchParam(key: string): string | null {
        return this.params.get(key) || null;
    }

    getAllSearchParams(): URLSearchParams {
        const urlParams = new URLSearchParams();
        this.params.forEach((value, key) => {
            urlParams.set(key, value);
        });
        return urlParams;
    }

    getCurrentPath(): string {
        return this.currentPath;
    }

    navigateTo(url: string): void {
        this.navigationHistory.push(url);
        
        // In real scenario, external URLs would do full page navigation
        // In tests, we just track the call
        if (url.startsWith('http://') || url.startsWith('https://')) {
            console.log('[MockRouting] External navigation (tracked):', url);
        } else {
            console.log('[MockRouting] Internal navigation (tracked):', url);
            this.currentPath = url;
        }
    }

    navigateReplace(path: string): void {
        this.replaceHistory.push(path);
        this.currentPath = path;
        console.log('[MockRouting] Replace URL (tracked):', path);
    }

    openPopup(url: string, name: string, features: string): Window | null {
        this.popupCalls.push({ url, name, features });
        console.log('[MockRouting] Popup open (tracked):', { url, name });
        
        // In tests, we don't actually open popups
        // Return a mock Window object if needed for testing
        return null;
    }
}
