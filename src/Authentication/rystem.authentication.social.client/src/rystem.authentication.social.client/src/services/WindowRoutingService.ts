import type { IRoutingService } from './IRoutingService';

/**
 * Default routing service using browser's native APIs
 * Uses window.location for URL reading and window.history for navigation
 * 
 * Works with:
 * - Vanilla React (no routing library)
 * - Standard browser navigation with full page reloads
 * - Server-side rendered apps
 * - Next.js Pages Router (with server redirects)
 * 
 * For client-side routing frameworks (React Router, Next.js App Router, Remix),
 * use framework-specific implementations instead.
 * 
 * For React Native, this will NOT work - provide a custom IRoutingService implementation.
 * 
 * @example
 * ```typescript
 * const routingService = new WindowRoutingService();
 * 
 * setupSocialLogin(x => {
 *     x.routingService = routingService; // Default: no config needed
 *     x.apiUri = 'https://api.example.com';
 * });
 * 
 * // Usage examples:
 * const code = routingService.getSearchParam('code'); // OAuth code
 * const currentPath = routingService.getCurrentPath(); // "/dashboard?tab=1"
 * routingService.navigateTo('https://oauth.provider.com/...'); // OAuth redirect
 * routingService.navigateReplace('/dashboard'); // Clean URL
 * ```
 */
export class WindowRoutingService implements IRoutingService {
    /**
     * Check if window is available (browser environment)
     */
    private isAvailable(): boolean {
        return typeof window !== 'undefined' && typeof window.location !== 'undefined';
    }

    /**
     * Get URL parameter from window.location.search
     * @param key - Parameter name
     * @returns Parameter value or null
     */
    getSearchParam(key: string): string | null {
        if (!this.isAvailable()) {
            console.warn('WindowRoutingService: window not available (React Native or SSR?)');
            return null;
        }
        const params = new URLSearchParams(window.location.search);
        return params.get(key);
    }

    /**
     * Get all URL parameters from window.location.search
     * @returns URLSearchParams object
     */
    getAllSearchParams(): URLSearchParams {
        if (!this.isAvailable()) {
            console.warn('WindowRoutingService: window not available (React Native or SSR?)');
            return new URLSearchParams();
        }
        return new URLSearchParams(window.location.search);
    }

    /**
     * Get current path from window.location
     * @returns Pathname + search query
     */
    getCurrentPath(): string {
        if (!this.isAvailable()) {
            console.warn('WindowRoutingService: window not available (React Native or SSR?)');
            return '/';
        }
        return window.location.pathname + window.location.search;
    }

    /**
     * Navigate to URL using window.location.href
     * Triggers full page reload/navigation
     * @param url - Full URL or relative path
     */
    navigateTo(url: string): void {
        if (!this.isAvailable()) {
            console.warn('WindowRoutingService: window not available (React Native or SSR?)');
            return;
        }
        window.location.href = url;
    }

    /**
     * Replace current URL using history.replaceState
     * Updates URL bar without page reload
     * @param path - New path to display
     */
    navigateReplace(path: string): void {
        if (!this.isAvailable()) {
            console.warn('WindowRoutingService: window not available (React Native or SSR?)');
            return;
        }
        if (typeof window.history !== 'undefined' && window.history.replaceState) {
            window.history.replaceState({}, '', path);
        }
    }

    /**
     * Open URL in popup window
     * @param url - URL to open
     * @param name - Window name
     * @param features - Window features string
     * @returns Window reference or null if blocked
     */
    openPopup(url: string, name: string, features: string): Window | null {
        if (!this.isAvailable() || typeof window.open !== 'function') {
            console.warn('WindowRoutingService: window.open not available (React Native or SSR?)');
            return null;
        }
        return window.open(url, name, features);
    }
}
