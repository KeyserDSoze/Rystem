import type { INavigationService } from './INavigationService';

/**
 * Default navigation service using browser's native window.location and window.history
 * 
 * Works with:
 * - Vanilla React (no routing library)
 * - Standard browser navigation
 * - Server-side rendered apps with full page reloads
 * - Next.js Pages Router (with server redirects)
 * 
 * For client-side routing frameworks (React Router, Next.js App Router, Remix),
 * use framework-specific implementations instead.
 * 
 * @example
 * ```typescript
 * const navigationService = new WindowNavigationService();
 * 
 * // Get current path
 * const path = navigationService.getCurrentPath(); // "/dashboard?tab=settings"
 * 
 * // Navigate to OAuth provider
 * navigationService.navigateTo('https://login.microsoftonline.com/oauth2/...');
 * 
 * // Clean URL after OAuth callback
 * navigationService.navigateReplace('/account/login');
 * 
 * // Open popup for OAuth
 * const popup = navigationService.openPopup(url, 'oauth', 'width=450,height=730');
 * ```
 */
export class WindowNavigationService implements INavigationService {
    /**
     * Get current path from browser's window.location
     * Includes pathname and search query string
     * 
     * @returns Current path with query parameters
     */
    getCurrentPath(): string {
        return window.location.pathname + window.location.search;
    }
    
    /**
     * Navigate to URL using window.location.href
     * Triggers full page reload/navigation
     * 
     * @param url - Full URL or relative path
     */
    navigateTo(url: string): void {
        window.location.href = url;
    }
    
    /**
     * Replace current URL using history.replaceState
     * Updates URL bar without page reload
     * 
     * @param path - New path to display in URL bar
     */
    navigateReplace(path: string): void {
        window.history.replaceState({}, '', path);
    }
    
    /**
     * Open URL in popup window using window.open
     * 
     * @param url - URL to open
     * @param name - Window name
     * @param features - Window features string
     * @returns Window reference or null if blocked
     */
    openPopup(url: string, name: string, features: string): Window | null {
        return window.open(url, name, features);
    }
}
