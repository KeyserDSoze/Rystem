/**
 * Interface for navigation abstraction
 * Allows framework-specific navigation implementations (React Router, Next.js, Remix, etc.)
 * 
 * This solves compatibility issues with client-side routing frameworks where
 * window.location and window.history operations bypass the router's internal state.
 * 
 * @example Default (Vanilla React)
 * ```typescript
 * const navService = new WindowNavigationService();
 * ```
 * 
 * @example React Router
 * ```typescript
 * const navService = new ReactRouterNavigationService();
 * // In component:
 * const navigate = useNavigate();
 * const location = useLocation();
 * navService.initialize(navigate, location);
 * ```
 */
export interface INavigationService {
    /**
     * Get current path including search parameters
     * Used for saving return URL before OAuth redirect
     * 
     * @returns Current route path with query string (e.g., "/dashboard?tab=settings")
     * 
     * @example
     * ```typescript
     * const currentPath = navigationService.getCurrentPath();
     * // Returns: "/account/profile?id=123"
     * ```
     */
    getCurrentPath(): string;
    
    /**
     * Navigate to a URL (for OAuth redirects and external navigation)
     * 
     * @param url - Full URL (https://...) or relative path (/login)
     * 
     * @example OAuth redirect
     * ```typescript
     * navigationService.navigateTo('https://login.microsoftonline.com/oauth2/authorize?...');
     * ```
     * 
     * @example Internal navigation
     * ```typescript
     * navigationService.navigateTo('/dashboard');
     * ```
     */
    navigateTo(url: string): void;
    
    /**
     * Replace current URL without full navigation (cleanup after OAuth callback)
     * This removes query parameters from the URL bar without triggering a page reload
     * 
     * @param path - Path to replace current URL with
     * 
     * @example Clean URL after OAuth
     * ```typescript
     * // Before: /account/login?code=ABC123&state=microsoft
     * navigationService.navigateReplace('/account/login');
     * // After: /account/login (query params removed)
     * ```
     */
    navigateReplace(path: string): void;
    
    /**
     * Open URL in popup window (for popup mode OAuth)
     * 
     * @param url - URL to open in popup
     * @param name - Window name/target
     * @param features - Window features (size, position, etc.)
     * @returns Window reference or null if blocked
     * 
     * @example
     * ```typescript
     * const popup = navigationService.openPopup(
     *     'https://oauth.provider.com/authorize',
     *     'oauth_popup',
     *     'width=450,height=730,top=100,left=100'
     * );
     * ```
     */
    openPopup(url: string, name: string, features: string): Window | null;
}
